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

    public static string? Resolve() =>
        Normalize(Environment.GetEnvironmentVariable("API_URL")) ?? Normalize(Embedded());

    private static string? Embedded() =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == MetadataKey)?.Value;

    private static string? Normalize(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null
        : url.Contains("://") ? url.Trim()
        : $"https://{url.Trim()}";
}
