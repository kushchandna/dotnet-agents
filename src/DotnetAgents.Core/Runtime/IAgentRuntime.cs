namespace DotnetAgents.Core.Runtime;

public interface IAgentRuntime
{
    IAsyncEnumerable<AgentStreamUpdate> RunStreamingAsync(
        AgentRunRequest request, CancellationToken ct = default);
}

public record AgentRunRequest(string UserId, string SessionId, string Message);
