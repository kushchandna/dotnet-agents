using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;
using DotnetAgents.Runtime;
using Microsoft.Extensions.DependencyInjection;

string? cliConfig = null;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i] == "--config") { cliConfig = args[i + 1]; break; }
var path = cliConfig
    ?? Environment.GetEnvironmentVariable("DOTNET_AGENTS_CONFIG")
    ?? "config.json";

AgentsConfig config;
try { config = await ConfigLoader.LoadAsync(path); }
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Config not found: {ex.FileName}");
    Console.Error.WriteLine("Usage: ConsoleSample [--config <path>]");
    return 1;
}

var errors = ConfigValidator.Validate(config);
if (errors.Count > 0)
{
    foreach (var e in errors) Console.Error.WriteLine($"[config] {e}");
    return 2;
}

var services = new ServiceCollection();
services.AddDotnetAgents(config);
var sp = services.BuildServiceProvider();

var sessions = sp.GetRequiredService<ISessionStore>();
var runtime = sp.GetRequiredService<IAgentRuntime>();

var user = config.Users[0];
var agent = config.Agents[0];
Console.WriteLine($"User: {user.DisplayName}   Agent: {agent.Name}");

var session = await sessions.CreateAsync(user.Id, agent.Id);
Console.WriteLine($"Session: {session.Id}");
Console.WriteLine("Type a message and press Enter. Ctrl+C to exit.");

while (true)
{
    Console.Write("\nYou: ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;

    Console.Write("Agent: ");
    await foreach (var update in runtime.RunStreamingAsync(new AgentRunRequest(user.Id, session.Id, line)))
    {
        switch (update)
        {
            case DeltaUpdate d: Console.Write(d.Content); break;
            case ToolCallUpdate tc: Console.Write($"\n  [call {tc.Name}({tc.Arguments})] "); break;
            case ToolResultUpdate tr: Console.Write($"\n  [result {tr.Name}: {tr.Result}] "); break;
            case ErrorUpdate e: Console.Write($"\n[error] {e.Message}"); break;
            case DoneUpdate: Console.WriteLine(); break;
        }
    }
}
