using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class HttpGetTool(IReadOnlyList<string> allowlist, IHttpClientFactory httpClientFactory) : IBuiltInTool
{
    public string Id => "http_get";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(FetchAsync, "http_get",
            "Performs an HTTP GET against an allowlisted URL and returns the response body as text.");

    private async Task<string> FetchAsync([Description("Absolute http(s) URL within the allowlist")] string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new UnauthorizedAccessException("URL is not a valid absolute http(s) URL");

        if (!allowlist.Any(entry => Matches(entry, uri)))
            throw new UnauthorizedAccessException($"URL '{url}' is not in the allowlist.");

        var client = httpClientFactory.CreateClient(nameof(HttpGetTool));
        return await client.GetStringAsync(uri);
    }

    private static bool Matches(string allowlistEntry, Uri uri)
    {
        if (!Uri.TryCreate(allowlistEntry, UriKind.Absolute, out var allowed))
            return false;
        return string.Equals(allowed.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(allowed.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
            && allowed.Port == uri.Port
            && uri.AbsolutePath.StartsWith(allowed.AbsolutePath, StringComparison.Ordinal);
    }
}
