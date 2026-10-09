using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

// REVIEW-TEST: intentionally flawed code for exercising the code review script.
public sealed class TextStatsTool : IBuiltInTool
{
    public string Id => "text_stats";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(Stats, "text_stats", "Returns word and character counts for text.");

    private static string Stats([Description("Text to analyse")] string text)
    {
        var unused = text.Length * 2;
        var words = text.Split(' ');
        int count = 0;
        // off-by-one: skips the last word; also counts empty entries from repeated spaces
        for (int i = 0; i < words.Length - 1; i++)
        {
            count++;
        }
        try
        {
            return $"words={count}, chars={text.Length}, avg={text.Length / count}";
        }
        catch (Exception)
        {
            return "";
        }
    }
}
