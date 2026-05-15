using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;
using DotnetAgents.Runtime.Providers;
using DotnetAgents.Tools.BuiltIn;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Runtime.Execution;

public sealed class AgentExecutor(
    IConfigurationService configurationService,
    IModelProviderFactory providerFactory,
    ISessionStore sessions,
    BuiltInToolRegistry tools) : IAgentRuntime
{
    public async IAsyncEnumerable<AgentStreamUpdate> RunStreamingAsync(
        AgentRunRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Resolve config + session
        var (session, agent, model, setupError) = await ResolveSetupAsync(request, ct);
        if (setupError is not null) { yield return new ErrorUpdate(setupError); yield break; }

        using var chatClient = providerFactory.Create(model!);

        // 2. Build MAF agent with tools and in-memory history provider
        var functions = tools.Resolve(agent!.Tools).ToList();
        var historyProvider = new InMemoryChatHistoryProvider();
        var mafAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatHistoryProvider = historyProvider,
            ChatOptions = new ChatOptions
            {
                Instructions = agent.SystemPrompt,
                Tools = functions.Count > 0 ? functions.Cast<AITool>().ToList() : null
            }
        });

        // 3. Create MAF session (client-side history — no conversationId) and seed with existing history
        AgentSession mafSession;
        string? sessionError = null;
        try
        {
            mafSession = await mafAgent.CreateSessionAsync(ct);
            var history = BuildMafHistory(session!);
            if (history.Count > 0) mafSession.SetInMemoryChatHistory(history);
        }
        catch (Exception ex)
        {
            sessionError = ex.Message;
            mafSession = null!;
        }
        if (sessionError is not null) { yield return new ErrorUpdate(sessionError); yield break; }

        // 4. Stream and map AgentResponseUpdate → AgentStreamUpdate
        //    Cannot yield inside try/catch, so we collect updates via IAsyncEnumerator.
        var newMessages = new List<SessionMessage>
        {
            new() { Id = NewId("usr"), Role = "user", Content = request.Message, Timestamp = DateTimeOffset.UtcNow }
        };

        var pendingCalls = new Dictionary<string, (string Name, string Args)>();
        var assistantText = new StringBuilder();
        var enumerator = mafAgent.RunStreamingAsync(request.Message, mafSession, null, ct)
                                 .GetAsyncEnumerator(ct);

        while (true)
        {
            AgentResponseUpdate? update = null;
            bool hasMore;
            string? iterError = null;
            try
            {
                hasMore = await enumerator!.MoveNextAsync();
                if (hasMore) update = enumerator.Current;
            }
            catch (Exception ex) { iterError = ex.Message; hasMore = false; }

            if (iterError is not null)
            {
                await enumerator!.DisposeAsync();
                yield return new ErrorUpdate(iterError);
                yield break;
            }

            if (!hasMore) break;

            foreach (var content in update!.Contents)
            {
                if (content is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                {
                    assistantText.Append(tc.Text);
                    yield return new DeltaUpdate(tc.Text);
                }
                else if (content is FunctionCallContent fc)
                {
                    var argsJson = fc.Arguments is null ? "{}" : JsonSerializer.Serialize(fc.Arguments);
                    yield return new ToolCallUpdate(fc.CallId, fc.Name, argsJson);
                    pendingCalls[fc.CallId] = (fc.Name, argsJson);
                }
                else if (content is FunctionResultContent fr)
                {
                    var result = fr.Result?.ToString() ?? string.Empty;
                    var name = string.Empty;
                    var args = "{}";
                    if (pendingCalls.TryGetValue(fr.CallId, out var p))
                    {
                        name = p.Name;
                        args = p.Args;
                        newMessages.Add(new SessionMessage
                        {
                            Id = NewId("tool"), Role = "tool",
                            ToolCalls = [new ToolCallRecord { CallId = fr.CallId, Name = name, Arguments = args, Result = result }],
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        pendingCalls.Remove(fr.CallId);
                    }
                    yield return new ToolResultUpdate(fr.CallId, name, result);
                }
            }
        }
        await enumerator!.DisposeAsync();

        // 5. Save final assistant message and persist session
        var asstId = NewId("ast");
        newMessages.Add(new SessionMessage
        {
            Id = asstId, Role = "assistant",
            Content = assistantText.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });

        var updated = session! with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            Title = session.Title ?? (request.Message.Length > 60 ? request.Message[..60] + "…" : request.Message),
            Messages = [.. session.Messages, .. newMessages]
        };
        await sessions.SaveAsync(updated, ct);

        yield return new DoneUpdate(asstId);
    }

    private async Task<(Session? session, AgentConfig? agent, ModelConfig? model, string? error)>
        ResolveSetupAsync(AgentRunRequest request, CancellationToken ct)
    {
        try
        {
            var session = await sessions.GetAsync(request.UserId, request.SessionId, ct)
                          ?? throw new InvalidOperationException(
                              $"Session '{request.SessionId}' not found for user '{request.UserId}'");
            var agent = configurationService.Config.Agents.FirstOrDefault(a => a.Id == session.AgentId)
                        ?? throw new InvalidOperationException($"Agent '{session.AgentId}' not found in config");
            var model = configurationService.Config.Models.FirstOrDefault(m => m.Id == agent.ModelId)
                        ?? throw new InvalidOperationException($"Model '{agent.ModelId}' not found in config");
            return (session, agent, model, null);
        }
        catch (Exception ex) { return (null, null, null, ex.Message); }
    }

    /// <summary>Converts stored session messages to MAF chat history (excludes system prompt — MAF handles that via ChatOptions.Instructions).</summary>
    private static List<ChatMessage> BuildMafHistory(Session session)
    {
        var msgs = new List<ChatMessage>();
        foreach (var m in session.Messages)
        {
            switch (m.Role)
            {
                case "user":      msgs.Add(new ChatMessage(ChatRole.User, m.Content ?? "")); break;
                case "assistant": msgs.Add(new ChatMessage(ChatRole.Assistant, m.Content ?? "")); break;
                case "tool":
                    if (m.ToolCalls is null) break;
                    msgs.Add(new ChatMessage(ChatRole.Assistant,
                        m.ToolCalls.Select(c => (AIContent)new FunctionCallContent(c.CallId, c.Name,
                            string.IsNullOrEmpty(c.Arguments) ? null
                            : JsonSerializer.Deserialize<Dictionary<string, object?>>(c.Arguments))).ToList()));
                    msgs.Add(new ChatMessage(ChatRole.Tool,
                        m.ToolCalls.Select(c => (AIContent)new FunctionResultContent(c.CallId, c.Result)).ToList()));
                    break;
            }
        }
        return msgs;
    }

    private static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid().ToString("N")[..10]}";
}
