using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;
using DotnetAgents.Runtime.Providers;
using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Runtime.Execution;

public sealed class AgentExecutor(
    IConfigurationService configurationService,
    IModelProviderFactory providerFactory,
    ISessionStore sessions,
    BuiltInToolRegistry tools) : IAgentRuntime
{
    private const int MaxToolIterations = 10;

    public async IAsyncEnumerable<AgentStreamUpdate> RunStreamingAsync(
        AgentRunRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Session? session = null;
        AgentConfig? agent = null;
        ModelConfig? model = null;
        IList<AIFunction>? functions = null;
        string? setupError = null;

        try
        {
            session = await sessions.GetAsync(request.UserId, request.SessionId, ct)
                      ?? throw new InvalidOperationException(
                          $"Session '{request.SessionId}' not found for user '{request.UserId}'");
            agent = configurationService.Config.Agents.FirstOrDefault(a => a.Id == session.AgentId)
                    ?? throw new InvalidOperationException($"Agent '{session.AgentId}' not found in config");
            model = configurationService.Config.Models.FirstOrDefault(m => m.Id == agent.ModelId)
                    ?? throw new InvalidOperationException($"Model '{agent.ModelId}' not found in config");
            functions = tools.Resolve(agent.Tools).ToList();
        }
        catch (Exception ex)
        {
            setupError = ex.Message;
        }

        if (setupError is not null)
        {
            yield return new ErrorUpdate(setupError);
            yield break;
        }

        using var chatClient = providerFactory.Create(model!);

        var chatMessages = BuildHistory(agent!, session!, request.Message);
        var newStored = new List<SessionMessage>
        {
            new() { Id = NewId("usr"), Role = "user", Content = request.Message, Timestamp = DateTimeOffset.UtcNow }
        };

        string? finalMessageId = null;
        var options = functions!.Count > 0
            ? new ChatOptions { Tools = functions.Cast<AITool>().ToList() }
            : null;

        for (int iter = 0; iter < MaxToolIterations; iter++)
        {
            var assistantText = new StringBuilder();
            var assistantContents = new List<AIContent>();

            IAsyncEnumerator<ChatResponseUpdate>? e = null;
            string? streamError = null;
            try
            {
                e = chatClient.GetStreamingResponseAsync(chatMessages, options, ct).GetAsyncEnumerator(ct);
            }
            catch (Exception ex) { streamError = ex.Message; }

            if (streamError is not null)
            {
                yield return new ErrorUpdate(streamError);
                yield break;
            }

            while (true)
            {
                ChatResponseUpdate? update = null;
                bool hasMore;
                string? iterError = null;
                try
                {
                    hasMore = await e!.MoveNextAsync();
                    if (hasMore) update = e.Current;
                }
                catch (Exception ex) { iterError = ex.Message; hasMore = false; }

                if (iterError is not null)
                {
                    await e!.DisposeAsync();
                    yield return new ErrorUpdate(iterError);
                    yield break;
                }

                if (!hasMore) break;

                foreach (var content in update!.Contents)
                {
                    assistantContents.Add(content);
                    if (content is TextContent tc)
                    {
                        assistantText.Append(tc.Text);
                        yield return new DeltaUpdate(tc.Text);
                    }
                    else if (content is FunctionCallContent fc)
                    {
                        var argsJson = fc.Arguments is null ? "{}" : JsonSerializer.Serialize(fc.Arguments);
                        yield return new ToolCallUpdate(fc.CallId, fc.Name, argsJson);
                    }
                }
            }
            await e!.DisposeAsync();

            chatMessages.Add(new ChatMessage(ChatRole.Assistant, assistantContents));

            var calls = assistantContents.OfType<FunctionCallContent>().ToList();
            if (calls.Count == 0)
            {
                var asstId = NewId("ast");
                newStored.Add(new SessionMessage
                {
                    Id = asstId, Role = "assistant",
                    Content = assistantText.ToString(),
                    Timestamp = DateTimeOffset.UtcNow
                });
                finalMessageId = asstId;
                break;
            }

            var toolResults = new List<AIContent>();
            var toolRecords = new List<ToolCallRecord>();
            foreach (var call in calls)
            {
                var fn = tools.Get(call.Name);
                string result;
                if (fn is null) result = $"Error: unknown tool '{call.Name}'";
                else
                {
                    try
                    {
                        var args = call.Arguments ?? new Dictionary<string, object?>();
                        var aiArgs = new AIFunctionArguments(args);
                        var output = await fn.InvokeAsync(aiArgs, ct);
                        result = output?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex) { result = $"Error: {ex.Message}"; }
                }

                toolResults.Add(new FunctionResultContent(call.CallId, result));
                toolRecords.Add(new ToolCallRecord
                {
                    CallId = call.CallId, Name = call.Name,
                    Arguments = call.Arguments is null ? "{}" : JsonSerializer.Serialize(call.Arguments),
                    Result = result
                });
                yield return new ToolResultUpdate(call.CallId, call.Name, result);
            }

            chatMessages.Add(new ChatMessage(ChatRole.Tool, toolResults));
            newStored.Add(new SessionMessage
            {
                Id = NewId("tool"), Role = "tool",
                ToolCalls = toolRecords, Timestamp = DateTimeOffset.UtcNow
            });
        }

        var updated = session! with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            Title = session!.Title ?? (request.Message.Length > 60 ? request.Message[..60] + "…" : request.Message),
            Messages = [.. session.Messages, .. newStored]
        };
        await sessions.SaveAsync(updated, ct);

        if (finalMessageId is null) yield return new ErrorUpdate("Tool iteration limit reached");
        else yield return new DoneUpdate(finalMessageId);
    }

    private List<ChatMessage> BuildHistory(AgentConfig agent, Session session, string userMessage)
    {
        var msgs = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
            msgs.Add(new ChatMessage(ChatRole.System, agent.SystemPrompt));

        foreach (var m in session.Messages)
        {
            switch (m.Role)
            {
                case "user":      msgs.Add(new ChatMessage(ChatRole.User, m.Content ?? "")); break;
                case "assistant": msgs.Add(new ChatMessage(ChatRole.Assistant, m.Content ?? "")); break;
                case "system":    msgs.Add(new ChatMessage(ChatRole.System, m.Content ?? "")); break;
                case "tool":
                    if (m.ToolCalls is null) break;
                    var calls = m.ToolCalls.Select(c => (AIContent)
                        new FunctionCallContent(c.CallId, c.Name,
                            string.IsNullOrEmpty(c.Arguments) ? null
                            : JsonSerializer.Deserialize<Dictionary<string, object?>>(c.Arguments))).ToList();
                    msgs.Add(new ChatMessage(ChatRole.Assistant, calls));
                    var results = m.ToolCalls.Select(c => (AIContent)
                        new FunctionResultContent(c.CallId, c.Result)).ToList();
                    msgs.Add(new ChatMessage(ChatRole.Tool, results));
                    break;
            }
        }

        msgs.Add(new ChatMessage(ChatRole.User, userMessage));
        return msgs;
    }

    private static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid().ToString("N")[..10]}";
}
