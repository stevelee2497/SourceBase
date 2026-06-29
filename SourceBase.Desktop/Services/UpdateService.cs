using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SourceBase.Desktop.Services;

public static class UpdateService
{
    private const string RepoOwner = "stevelee2497";
    private const string RepoName = "SourceBase";
    private const string TagPrefix = "desktop-v";
    private const string AssetPrefix = "Jupiter-v";
    private const string AssetSuffix = ".exe";

    private static readonly HttpClient Http = CreateHttp();

    public record UpdateInfo(string Version, string DownloadUrl);

    public static string CurrentVersion =>
        Assembly.GetEntryAssembly()!
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0";

    public static UpdateInfo? PendingUpdate { get; private set; }

    public static async Task CheckAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases";
            var json = await Http.GetStringAsync(url);
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json) ?? [];

            var latest = releases
                .Where(r => r.TagName.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)
                         && !r.Draft && !r.Prerelease)
                .OrderByDescending(r => ParseVersion(r.TagName[TagPrefix.Length..]))
                .FirstOrDefault();

            if (latest is null) return;

            var latestVer = ParseVersion(latest.TagName[TagPrefix.Length..]);
            var currentVer = ParseVersion(CurrentVersion);
            if (latestVer is null || currentVer is null || latestVer <= currentVer) return;

            var asset = latest.Assets.FirstOrDefault(a =>
                a.Name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase));
            if (asset is null) return;

            PendingUpdate = new UpdateInfo(latest.TagName[TagPrefix.Length..], asset.BrowserDownloadUrl);
        }
        catch { /* best-effort — silent on any network or parse error */ }
    }

    public static async Task ApplyUpdateAsync(Action<int> onProgress)
    {
        if (PendingUpdate is null) return;

        var exePath = Process.GetCurrentProcess().MainModule!.FileName;
        var tempExe = Path.Combine(Path.GetTempPath(), "SourceBase.Desktop.new.exe");
        var batPath = Path.Combine(Path.GetTempPath(), "sb_update.bat");

        using var resp = await Http.GetAsync(PendingUpdate.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? 0L;
        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var dst = File.Create(tempExe);
        var buf = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await src.ReadAsync(buf)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, read));
            downloaded += read;
            if (total > 0) onProgress((int)(downloaded * 100 / total));
        }

        File.WriteAllText(batPath,
            $"@echo off\r\n" +
            $"timeout /t 2 /nobreak >nul\r\n" +
            $"move /y \"{tempExe}\" \"{exePath}\"\r\n" +
            $"start \"\" \"{exePath}\"\r\n" +
            $"del \"%~f0\"\r\n");

        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{batPath}\"") { WindowStyle = ProcessWindowStyle.Hidden });
        System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Application.Current.Shutdown);
    }

    private static Version? ParseVersion(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var normalized = s.Contains('.') ? s : $"{s}.0";
        return Version.TryParse(normalized, out var v) ? v : null;
    }

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SourceBase-Desktop/1.0");
        return client;
    }

    private record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] GitHubAsset[] Assets);

    private record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}
