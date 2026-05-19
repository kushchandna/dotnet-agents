using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Skills;

public interface ISkillDiscoveryService
{
    Task<IReadOnlyList<SkillInfo>> GetAllSkillsAsync(IEnumerable<string> directories, CancellationToken ct);
}
