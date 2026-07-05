namespace SourceBase.Desktop.Config;

/// <summary>
/// Resolves the API base URL from configuration — never hardcoded and never user-facing.
/// Priority: runtime <c>API_URL</c> env var (local dev via <see cref="DotEnv"/>) →
/// build-time <see cref="BuildConfig.ApiBaseUrl"/> (injected from the API_URL repo variable
/// in desktop-publish.yml). A bare host is normalized to https://; blank resolves to null,
/// which leaves API sync disabled.
/// </summary>
internal static class ApiUrl
{
    public static string? Resolve() =>
        Normalize(Environment.GetEnvironmentVariable("API_URL") ?? BuildConfig.ApiBaseUrl);

    private static string? Normalize(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null
        : url.Contains("://") ? url.Trim()
        : $"https://{url.Trim()}";
}
