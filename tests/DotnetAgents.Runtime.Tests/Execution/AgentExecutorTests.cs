using System.Runtime.CompilerServices;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;
using DotnetAgents.Core.Skills;
using DotnetAgents.Runtime.Execution;
using DotnetAgents.Runtime.Mcp;
using DotnetAgents.Runtime.Providers;
using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace DotnetAgents.Runtime.Tests.Execution;

[TestFixture]
public class AgentExecutorTests
{
    private string _sessionsRoot = null!;
    private JsonSessionStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _sessionsRoot = Path.Combine(Path.GetTempPath(), "exec-" + Guid.NewGuid());
        Directory.CreateDirectory(_sessionsRoot);
        _store = new JsonSessionStore(_sessionsRoot);
    }

    [TearDown]
    public void TearDown() { if (Directory.Exists(_sessionsRoot)) Directory.Delete(_sessionsRoot, true); }

    private sealed class StubChatClient(Func<IList<ChatMessage>, ChatOptions?, IAsyncEnumerable<ChatResponseUpdate>> stream)
        : IChatClient
    {
        // Convenience constructor for tests that don't need options
        public StubChatClient(Func<IList<ChatMessage>, IAsyncEnumerable<ChatResponseUpdate>> stream)
            : this((msgs, _) => stream(msgs)) { }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
            => stream(messages.ToList(), options);
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class StubFactory(IChatClient client) : IModelProviderFactory
    {
        public IChatClient Create(ModelConfig model) => client;
    }

    private static ISkillDiscoveryService NoSkills()
    {
        var svc = Substitute.For<ISkillDiscoveryService>();
        svc.GetAllSkillsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult<IReadOnlyList<SkillInfo>>([]));
        return svc;
    }

    private AgentsConfig BuildConfig() => new()
    {
        Agents = [new AgentConfig {
            Id="a", Name="A", ModelId="m", SystemPrompt="be helpful",
            Tools=["echo","get_current_time"] }],
        Models = [new ModelConfig { Id="m", Provider=ModelProvider.OpenAI, ModelName="gpt-4o", ApiKeyEnvVar="K" }],
        Users  = [new UserConfig { Id="u", DisplayName="U" }],
        Sessions = new SessionsConfig { Directory=_sessionsRoot }
    };

    [Test]
    public async Task RunStreaming_PlainText_YieldsDeltasAndDone()
    {
        async IAsyncEnumerable<ChatResponseUpdate> Stream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello ");
            yield return new ChatResponseUpdate(ChatRole.Assistant, "world");
            await Task.CompletedTask;
        }

        var client = new StubChatClient(_ => Stream());
        var cfg = BuildConfig();
        var session = await _store.CreateAsync("u", "a");
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);
        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        var updates = new List<AgentStreamUpdate>();
        await foreach (var u in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "Hi")))
            updates.Add(u);

        Assert.That(updates.OfType<DeltaUpdate>().Select(d => d.Content), Is.EquivalentTo(new[] { "Hello ", "world" }));
        Assert.That(updates.OfType<DoneUpdate>().Count(), Is.EqualTo(1));

        var loaded = await _store.GetAsync("u", session.Id);
        Assert.That(loaded!.Messages, Has.Count.EqualTo(2));
        Assert.That(loaded.Messages[0].Role, Is.EqualTo("user"));
        Assert.That(loaded.Messages[1].Role, Is.EqualTo("assistant"));
        Assert.That(loaded.Messages[1].Content, Is.EqualTo("Hello world"));
    }

    [Test]
    public async Task RunStreaming_ToolCall_ExecutesAndContinues()
    {
        var phase = 0;
        IAsyncEnumerable<ChatResponseUpdate> Stream(IList<ChatMessage> _) => phase++ == 0 ? FirstPass() : SecondPass();

        async IAsyncEnumerable<ChatResponseUpdate> FirstPass([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant,
                new[] { new FunctionCallContent("call1", "echo", new Dictionary<string, object?> { ["text"] = "hi" }) });
            await Task.CompletedTask;
        }

        async IAsyncEnumerable<ChatResponseUpdate> SecondPass([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Echoed: hi");
            await Task.CompletedTask;
        }

        var client = new StubChatClient(Stream);
        var cfg = BuildConfig();
        var session = await _store.CreateAsync("u", "a");
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);
        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        var updates = new List<AgentStreamUpdate>();
        await foreach (var u in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "say hi")))
            updates.Add(u);

        Assert.That(updates.OfType<ToolCallUpdate>().Count(), Is.EqualTo(1));
        Assert.That(updates.OfType<ToolResultUpdate>().Single().Result, Is.EqualTo("hi"));
        Assert.That(updates.OfType<DeltaUpdate>().Single().Content, Is.EqualTo("Echoed: hi"));
        Assert.That(updates.OfType<DoneUpdate>().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task RunStreaming_UnknownSession_YieldsError()
    {
        var client = Substitute.For<IChatClient>();
        var cfg = BuildConfig();
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);
        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        var updates = new List<AgentStreamUpdate>();
        await foreach (var u in executor.RunStreamingAsync(new AgentRunRequest("u", "missing", "hi")))
            updates.Add(u);

        Assert.That(updates.OfType<ErrorUpdate>().Any(), Is.True);
    }

    [Test]
    public async Task RunStreaming_ReplaysSessionHistory_IntoChatMessages()
    {
        IList<ChatMessage> capturedMessages = null!;
        ChatOptions? capturedOptions = null;

        async IAsyncEnumerable<ChatResponseUpdate> Stream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok") { FinishReason = ChatFinishReason.Stop };
            await Task.CompletedTask;
        }

        var client = new StubChatClient((msgs, opts) => { capturedMessages = msgs.ToList(); capturedOptions = opts; return Stream(); });
        var cfg = BuildConfig();

        var session = await _store.CreateAsync("u", "a");
        var seeded = session with
        {
            Messages = [
                new SessionMessage { Id="m1", Role="user", Content="hello", Timestamp=DateTimeOffset.UtcNow },
                new SessionMessage { Id="m2", Role="assistant", Content="hi", Timestamp=DateTimeOffset.UtcNow },
                new SessionMessage {
                    Id="m3", Role="tool", Timestamp=DateTimeOffset.UtcNow,
                    ToolCalls=[new ToolCallRecord { CallId="c1", Name="echo", Arguments="{\"text\":\"x\"}", Result="x" }]
                }
            ]
        };
        await _store.SaveAsync(seeded);

        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);
        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        await foreach (var _ in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "next turn"))) { }

        Assert.That(capturedMessages, Is.Not.Null);
        // MAF manages history via InMemoryChatHistoryProvider; the system prompt goes through
        // ChatOptions.Instructions, not as a message. Expected order:
        //   user(hello) + assistant(hi) + assistant(FunctionCall) + tool(FunctionResult) + user(next turn)
        Assert.That(capturedMessages.Count, Is.EqualTo(5));
        Assert.That(capturedOptions?.Instructions, Is.EqualTo("be helpful"));
        Assert.That(capturedMessages[0].Role, Is.EqualTo(ChatRole.User));
        Assert.That(capturedMessages[1].Role, Is.EqualTo(ChatRole.Assistant));
        Assert.That(capturedMessages[2].Role, Is.EqualTo(ChatRole.Assistant));
        Assert.That(capturedMessages[2].Contents.OfType<FunctionCallContent>().Any(), Is.True);
        Assert.That(capturedMessages[3].Role, Is.EqualTo(ChatRole.Tool));
        Assert.That(capturedMessages[3].Contents.OfType<FunctionResultContent>().Any(), Is.True);
        Assert.That(capturedMessages[4].Role, Is.EqualTo(ChatRole.User));
        Assert.That(capturedMessages[4].Text, Is.EqualTo("next turn"));
    }

    [Test]
    public async Task RunStreaming_ToolLoop_TerminatesAndSavesSession()
    {
        // MAF handles the tool loop internally with its own iteration limit.
        // The loop completes normally (DoneUpdate) and the session is persisted.
        int callCount = 0;
        async IAsyncEnumerable<ChatResponseUpdate> AlwaysToolCall([EnumeratorCancellation] CancellationToken ct = default)
        {
            callCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant,
                new[] { new FunctionCallContent($"call{callCount}", "echo",
                    new Dictionary<string, object?> { ["text"] = "spam" }) })
            {
                FinishReason = ChatFinishReason.ToolCalls
            };
            await Task.CompletedTask;
        }

        var client = new StubChatClient(_ => AlwaysToolCall());
        var cfg = BuildConfig();
        var session = await _store.CreateAsync("u", "a");
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);
        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        var updates = new List<AgentStreamUpdate>();
        await foreach (var u in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "go")))
            updates.Add(u);

        // MAF runs its own loop and then terminates normally.
        // The last LLM call may produce a tool call without a result (loop limit hit mid-turn).
        Assert.That(callCount, Is.GreaterThan(0), "underlying client should have been called");
        Assert.That(updates.OfType<ToolCallUpdate>().Count(), Is.GreaterThan(0));
        Assert.That(updates.OfType<ToolResultUpdate>().Count(), Is.GreaterThan(0));
        Assert.That(updates.OfType<DoneUpdate>().Count(), Is.EqualTo(1), "MAF terminates with DoneUpdate not ErrorUpdate");
        Assert.That(updates.OfType<ErrorUpdate>().Any(), Is.False, "no error when MAF terminates the loop");

        var loaded = await _store.GetAsync("u", session.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Messages.Count, Is.GreaterThanOrEqualTo(2), "user message + tool turns + assistant persisted");
    }

    [Test]
    public async Task RunStreamingAsync_MergesMcpToolsWithBuiltInTools()
    {
        async IAsyncEnumerable<ChatResponseUpdate> Stream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "done");
            await Task.CompletedTask;
        }

        var client = new StubChatClient(_ => Stream());
        var cfg = BuildConfig(); // agent has tools: ["echo", "get_current_time"]
        var session = await _store.CreateAsync("u", "a");
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());

        var mcpTool = AIFunctionFactory.Create((string x) => x, "mcpEcho");
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([mcpTool]);

        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, NoSkills());

        await foreach (var _ in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "hi"))) { }

        // Verify the merge path was exercised: ResolveToolsForAgent was called with the resolved agent
        mcpManager.Received(1).ResolveToolsForAgent(Arg.Is<AgentConfig>(a => a.Id == "a"));
    }

    [Test]
    public async Task RunStreaming_CustomSkills_InjectsSkillContentIntoSystemPrompt()
    {
        ChatOptions? capturedOptions = null;

        async IAsyncEnumerable<ChatResponseUpdate> Stream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok") { FinishReason = ChatFinishReason.Stop };
            await Task.CompletedTask;
        }

        var client = new StubChatClient((msgs, opts) => { capturedOptions = opts; return Stream(); });

        var cfg = new AgentsConfig
        {
            Agents = [new AgentConfig {
                Id = "a", Name = "A", ModelId = "m", SystemPrompt = "be helpful",
                Tools = [],
                SkillsInheritance = SkillsInheritance.Custom,
                Skills = ["coding"] }],
            Models = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "K" }],
            Users  = [new UserConfig { Id = "u", DisplayName = "U" }],
            Sessions = new SessionsConfig { Directory = _sessionsRoot }
        };

        var session = await _store.CreateAsync("u", "a");
        var registry = new BuiltInToolRegistry(null, Substitute.For<IHttpClientFactory>());
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.ResolveToolsForAgent(Arg.Any<AgentConfig>()).Returns([]);

        var skillDiscovery = Substitute.For<ISkillDiscoveryService>();
        skillDiscovery.GetAllSkillsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SkillInfo>>(
            [
                new SkillInfo("coding", "Coding assistance", "Always write clean code.", "/skills/coding.md"),
                new SkillInfo("writing", "Writing help", "Write clearly.", "/skills/writing.md")
            ]));

        var executor = new AgentExecutor(new ConfigurationService(cfg), new StubFactory(client), _store, registry, mcpManager, skillDiscovery);

        await foreach (var _ in executor.RunStreamingAsync(new AgentRunRequest("u", session.Id, "help"))) { }

        Assert.That(capturedOptions?.Instructions, Is.EqualTo("be helpful\n\nAlways write clean code."));
    }
}
