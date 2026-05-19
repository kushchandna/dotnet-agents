using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Skills;

public class SkillDiscoveryService : ISkillDiscoveryService
{
    public async Task<IReadOnlyList<SkillInfo>> GetAllSkillsAsync(IEnumerable<string> directories, CancellationToken ct)
    {
        var skills = new List<SkillInfo>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;

            var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var skill = await TryParseSkillAsync(file, ct);
                if (skill is not null)
                    skills.Add(skill);
            }
        }

        return skills;
    }

    private static async Task<SkillInfo?> TryParseSkillAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, ct);
            return ParseSkill(filePath, lines);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    internal static SkillInfo ParseSkill(string filePath, string[] lines)
    {
        string? name = null;
        string? description = null;
        int contentStart = 0;

        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            int closingIndex = -1;
            string? parsedName = null;
            string? parsedDescription = null;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    closingIndex = i;
                    break;
                }

                var colonIndex = lines[i].IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = lines[i][..colonIndex].Trim();
                    var value = lines[i][(colonIndex + 1)..].Trim();

                    if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
                        parsedName = value;
                    else if (key.Equals("description", StringComparison.OrdinalIgnoreCase))
                        parsedDescription = value;
                }
            }

            if (closingIndex >= 0)
            {
                name = parsedName;
                description = parsedDescription;
                contentStart = closingIndex + 1;
            }
        }

        var id = !string.IsNullOrWhiteSpace(name)
            ? name
            : Path.GetFileNameWithoutExtension(filePath);

        var content = string.Join('\n', lines[contentStart..]).TrimStart('\n');

        return new SkillInfo(id, description ?? string.Empty, content, filePath);
    }
}
