using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Sessions;

public interface ISessionStore
{
    Task<Session>                       CreateAsync(string userId, string agentId, CancellationToken ct = default);
    Task<Session?>                      GetAsync(string userId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<Session>>        ListAsync(string userId, CancellationToken ct = default);
    Task                                DeleteAsync(string userId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionMessage>> GetMessagesAsync(string userId, string sessionId, CancellationToken ct = default);
    Task                                SaveAsync(Session session, CancellationToken ct = default);
}
