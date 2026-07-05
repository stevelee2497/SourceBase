using System.IO;

namespace SourceBase.Desktop.Config;

/// <summary>
/// Minimal .env loader for local development. Reads KEY=VALUE lines from a `.env` file
/// next to the executable (or the current directory when run via `dotnet run`) and sets
/// them as process environment variables — so devs can drop the API URL into a git-ignored
/// file instead of editing settings or exporting shell vars. No-op in published builds
/// where no .env is shipped. Never throws — a missing/malformed file is silently ignored.
/// </summary>
internal static class DotEnv
{
    public static void Load()
    {
        try
        {
            foreach (var path in CandidatePaths())
            {
                if (!File.Exists(path)) continue;
                ApplyFile(path);
                return; // first found wins
            }
        }
        catch { /* dev convenience only — never break startup */ }
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, ".env");
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
    }

    private static void ApplyFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
