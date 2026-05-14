using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class ReadFileTool(string sandboxRoot) : IBuiltInTool
{
    private readonly string _root = Path.GetFullPath(sandboxRoot);

    public string Id => "read_file";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(ReadFile, "read_file",
            "Reads a UTF-8 file from the sandbox root. Path is resolved relative to the sandbox.");

    private string ReadFile([Description("Path relative to the sandbox root")] string path)
    {
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        var combined = Path.IsPathRooted(path) ? path : Path.Combine(_root, path);
        var full = Path.GetFullPath(combined);
        if (!full.StartsWith(rootWithSep, StringComparison.Ordinal) && full != _root)
            throw new UnauthorizedAccessException($"Path '{path}' escapes the sandbox.");
        return File.ReadAllText(full);
    }
}
