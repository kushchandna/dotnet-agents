using System.Text.Json;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Sessions;

public sealed class JsonSessionStore(string rootDirectory) : ISessionStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string UserDir(string userId) => Path.Combine(rootDirectory, userId);
    private string FilePath(string userId, string id) => Path.Combine(UserDir(userId), id + ".json");

    public async Task<Session> CreateAsync(string userId, string agentId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new Session
        {
            Id = "sess_" + Guid.NewGuid().ToString("N")[..12],
            UserId = userId,
            AgentId = agentId,
            CreatedAt = now,
            UpdatedAt = now,
            Messages = []
        };
        Directory.CreateDirectory(UserDir(userId));
        await SaveAsync(session, ct);
        return session;
    }

    public async Task<Session?> GetAsync(string userId, string sessionId, CancellationToken ct = default)
    {
        var path = FilePath(userId, sessionId);
        if (!File.Exists(path)) return null;
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Session>(fs, ConfigLoader.JsonOptions, ct);
    }

    public async Task<IReadOnlyList<Session>> ListAsync(string userId, CancellationToken ct = default)
    {
        var dir = UserDir(userId);
        if (!Directory.Exists(dir)) return [];
        var list = new List<Session>();
        foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
        {
            await using var fs = File.OpenRead(path);
            var s = await JsonSerializer.DeserializeAsync<Session>(fs, ConfigLoader.JsonOptions, ct);
            if (s is not null) list.Add(s);
        }
        return list.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public Task DeleteAsync(string userId, string sessionId, CancellationToken ct = default)
    {
        var path = FilePath(userId, sessionId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SessionMessage>> GetMessagesAsync(
        string userId, string sessionId, CancellationToken ct = default)
        => (await GetAsync(userId, sessionId, ct))?.Messages ?? [];

    public async Task SaveAsync(Session session, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(UserDir(session.UserId));
            var path = FilePath(session.UserId, session.Id);
            var tmp = path + ".tmp";
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, session, ConfigLoader.JsonOptions, ct);
            File.Move(tmp, path, overwrite: true);
        }
        finally { _lock.Release(); }
    }
}
