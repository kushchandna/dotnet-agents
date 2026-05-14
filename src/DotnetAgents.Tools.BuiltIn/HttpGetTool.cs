using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class HttpGetTool(IReadOnlyList<string> allowlist, IHttpClientFactory httpClientFactory) : IBuiltInTool
{
    public string Id => "http_get";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(FetchAsync, "http_get",
            "Performs an HTTP GET against an allowlisted URL and returns the response body as text.");

    private async Task<string> FetchAsync([Description("Absolute URL; must start with an allowlisted prefix")] string url)
    {
        if (!allowlist.Any(a => url.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"URL '{url}' is not in the allowlist.");
        var client = httpClientFactory.CreateClient(nameof(HttpGetTool));
        return await client.GetStringAsync(new Uri(url));
    }
}
