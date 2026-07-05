using System.Reflection;

namespace SourceBase.Desktop.Config;

/// <summary>
/// Resolves the API base URL — never hardcoded in tracked source and never user-facing.
/// Precedence: the <c>API_URL</c> environment variable (dev override via shell or a git-ignored
/// <c>.env</c> loaded by <see cref="DotEnv"/>) first, then the value embedded at publish time as
/// <c>[AssemblyMetadata("ApiUrl", ...)]</c> (set from the API_URL repo variable in
/// desktop-publish.yml). A bare host is normalized to https://; blank resolves to null, which
/// leaves API sync disabled.
/// </summary>
internal static class ApiUrl
{
    public const string MetadataKey = "ApiUrl";

    public static string? Resolve()
    {
        var env = Normalize(Environment.GetEnvironmentVariable("API_URL"));
        if (env is not null)
        {
            Log.Info($"ApiUrl resolved from API_URL env var: {env}");
            return env;
        }

        var embedded = Normalize(Embedded());
        if (embedded is not null)
        {
            Log.Info($"ApiUrl resolved from embedded assembly metadata: {embedded}");
            return embedded;
        }

        Log.Warn("ApiUrl unresolved — no API_URL env var and no embedded metadata; API sync disabled.");
        return null;
    }

    private static string? Embedded() =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == MetadataKey)?.Value;

    private static string? Normalize(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null
        : url.Contains("://") ? url.Trim()
        : $"https://{url.Trim()}";
}
