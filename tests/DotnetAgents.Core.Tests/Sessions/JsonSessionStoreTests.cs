using DotnetAgents.Core.Models;
using DotnetAgents.Core.Sessions;

namespace DotnetAgents.Core.Tests.Sessions;

[TestFixture]
public class JsonSessionStoreTests
{
    private string _root = null!;
    private JsonSessionStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "agents-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
        _store = new JsonSessionStore(_root);
    }

    [TearDown]
    public void TearDown() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [Test]
    public async Task Create_Persists_AndIsRetrievable()
    {
        var s = await _store.CreateAsync("alice", "assistant");
        Assert.That(s.UserId, Is.EqualTo("alice"));
        Assert.That(File.Exists(Path.Combine(_root, "alice", s.Id + ".json")), Is.True);
        var got = await _store.GetAsync("alice", s.Id);
        Assert.That(got, Is.Not.Null);
        Assert.That(got!.AgentId, Is.EqualTo("assistant"));
    }

    [Test]
    public async Task List_ReturnsOnlyForUser_OrderedByUpdatedDesc()
    {
        var older = await _store.CreateAsync("alice", "a");
        await Task.Delay(10);
        var newer = await _store.CreateAsync("alice", "a");
        await _store.CreateAsync("bob", "a");

        var alice = await _store.ListAsync("alice");
        Assert.That(alice, Has.Count.EqualTo(2));
        Assert.That(alice[0].Id, Is.EqualTo(newer.Id));
        Assert.That(alice[1].Id, Is.EqualTo(older.Id));
    }

    [Test]
    public async Task Get_MissingSession_ReturnsNull()
    {
        Assert.That(await _store.GetAsync("alice", "nope"), Is.Null);
    }

    [Test]
    public async Task Delete_RemovesFile()
    {
        var s = await _store.CreateAsync("alice", "a");
        await _store.DeleteAsync("alice", s.Id);
        Assert.That(await _store.GetAsync("alice", s.Id), Is.Null);
    }

    [Test]
    public async Task Save_PersistsMessages_AndRoundtrips()
    {
        var s = await _store.CreateAsync("alice", "a");
        var updated = s with {
            UpdatedAt = DateTimeOffset.UtcNow,
            Title = "hello",
            Messages = [
                new SessionMessage{Id="m1",Role="user",Content="hi",Timestamp=DateTimeOffset.UtcNow},
                new SessionMessage{
                    Id="m2",Role="tool",Timestamp=DateTimeOffset.UtcNow,
                    ToolCalls=[new ToolCallRecord{CallId="c1",Name="echo",Arguments="{}",Result="hi"}]}
            ]
        };
        await _store.SaveAsync(updated);

        var got = await _store.GetAsync("alice", s.Id);
        Assert.That(got!.Title, Is.EqualTo("hello"));
        Assert.That(got.Messages, Has.Count.EqualTo(2));
        Assert.That(got.Messages[1].ToolCalls![0].Name, Is.EqualTo("echo"));
    }

    [Test]
    public async Task List_NoUserDir_ReturnsEmpty()
    {
        var list = await _store.ListAsync("ghost");
        Assert.That(list, Is.Empty);
    }
}
