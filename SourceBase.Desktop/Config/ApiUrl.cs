namespace SourceBase.Desktop.Config;

/// <summary>
/// Resolves the API base URL from the <c>API_URL</c> environment variable — never hardcoded
/// and never user-facing. In CI it's set from the API_URL repo variable (desktop-publish.yml);
/// in local dev it comes from a git-ignored <c>.env</c> loaded by <see cref="DotEnv"/>.
/// A bare host is normalized to https://; blank resolves to null, which leaves API sync disabled.
/// </summary>
internal static class ApiUrl
{
    public static string? Resolve() => Normalize(Environment.GetEnvironmentVariable("API_URL"));

    private static string? Normalize(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null
        : url.Contains("://") ? url.Trim()
        : $"https://{url.Trim()}";
}
