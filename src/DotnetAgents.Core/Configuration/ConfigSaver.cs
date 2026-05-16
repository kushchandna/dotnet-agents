using System.Text.Json;
using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Configuration;
public sealed class ConfigSaver(string path) : IConfigSaver
{
    public async Task SaveAsync(AgentsConfig config, CancellationToken ct = default)
    {
        var tmp = path + ".tmp";
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, config, ConfigLoader.JsonOptions, ct);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            throw;
        }
    }
}
