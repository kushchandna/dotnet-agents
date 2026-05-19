using DotnetAgents.Core.Skills;

namespace DotnetAgents.Core.Tests.Skills;

[TestFixture]
public class SkillDiscoveryServiceTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "skill-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    // ParseSkill tests

    [Test]
    public void ParseSkill_ValidFrontmatter_ReturnsCorrectFields()
    {
        var filePath = Path.Combine(_dir, "my-skill.md");
        var lines = new[]
        {
            "---",
            "name: My Skill",
            "description: Does something useful",
            "---",
            "",
            "# Body",
            "Content here."
        };

        var result = SkillDiscoveryService.ParseSkill(filePath, lines);

        Assert.That(result.Id, Is.EqualTo("My Skill"));
        Assert.That(result.Description, Is.EqualTo("Does something useful"));
        Assert.That(result.Content, Is.EqualTo("# Body\nContent here."));
        Assert.That(result.FilePath, Is.EqualTo(filePath));
    }

    [Test]
    public void ParseSkill_NoFrontmatter_UsesFilenameAndEmptyDescription()
    {
        var filePath = Path.Combine(_dir, "plain-skill.md");
        var lines = new[]
        {
            "# Just a heading",
            "No frontmatter here."
        };

        var result = SkillDiscoveryService.ParseSkill(filePath, lines);

        Assert.That(result.Id, Is.EqualTo("plain-skill"));
        Assert.That(result.Description, Is.EqualTo(string.Empty));
        Assert.That(result.Content, Is.EqualTo("# Just a heading\nNo frontmatter here."));
        Assert.That(result.FilePath, Is.EqualTo(filePath));
    }

    [Test]
    public void ParseSkill_UnclosedFrontmatter_TreatedAsNoFrontmatter()
    {
        var filePath = Path.Combine(_dir, "unclosed.md");
        var lines = new[]
        {
            "---",
            "name: Ghost Skill",
            "description: Should be ignored",
            "# No closing delimiter"
        };

        var result = SkillDiscoveryService.ParseSkill(filePath, lines);

        Assert.That(result.Id, Is.EqualTo("unclosed"));
        Assert.That(result.Description, Is.EqualTo(string.Empty));
        // full file text is the content
        Assert.That(result.Content, Does.Contain("---"));
        Assert.That(result.Content, Does.Contain("name: Ghost Skill"));
    }

    [Test]
    public void ParseSkill_EmptyFile_ReturnsFilenameId()
    {
        var filePath = Path.Combine(_dir, "empty.md");
        var result = SkillDiscoveryService.ParseSkill(filePath, []);

        Assert.That(result.Id, Is.EqualTo("empty"));
        Assert.That(result.Description, Is.EqualTo(string.Empty));
        Assert.That(result.Content, Is.EqualTo(string.Empty));
    }

    // Integration tests using the real service with a temp directory

    [Test]
    public async Task GetAllSkillsAsync_ReturnsSkillsFromDirectory()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "skill-a.md"),
            "---\nname: Skill A\ndescription: First\n---\n\nBody A");
        await File.WriteAllTextAsync(Path.Combine(_dir, "skill-b.md"),
            "Just plain content.");

        var service = new SkillDiscoveryService();
        var skills = await service.GetAllSkillsAsync([_dir], CancellationToken.None);

        Assert.That(skills, Has.Count.EqualTo(2));
        var a = skills.Single(s => s.Id == "Skill A");
        Assert.That(a.Description, Is.EqualTo("First"));
        Assert.That(a.Content, Is.EqualTo("Body A"));

        var b = skills.Single(s => s.Id == "skill-b");
        Assert.That(b.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetAllSkillsAsync_MissingDirectory_ReturnsEmpty()
    {
        var service = new SkillDiscoveryService();
        var skills = await service.GetAllSkillsAsync(["/nonexistent/path"], CancellationToken.None);

        Assert.That(skills, Is.Empty);
    }

    [Test]
    public async Task GetAllSkillsAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "skill.md"),
            "---\nname: X\n---\n\nbody");

        var service = new SkillDiscoveryService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        OperationCanceledException? caught = null;
        try
        {
            await service.GetAllSkillsAsync([_dir], cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            caught = ex;
        }
        Assert.That(caught, Is.Not.Null);
    }
}
