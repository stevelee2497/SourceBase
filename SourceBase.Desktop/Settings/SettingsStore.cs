using System.IO;
using System.Text.Json;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Settings;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> as JSON under
/// %AppData%\SourceBase.Desktop\settings.json — mirrors BreakTimer's %AppData% storage.
/// </summary>
public sealed class SettingsStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SourceBase.Desktop");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                if (loaded is not null)
                {
                    // Guard against an empty habit list from a hand-edited file.
                    if (loaded.Habits.Count == 0)
                        loaded.Habits = AppSettings.DefaultHabits();
                    Current = loaded;
                }
            }
            else
            {
                Save(); // write defaults on first run
            }
        }
        catch
        {
            // Corrupt file → fall back to defaults rather than crashing on startup.
            Current = new AppSettings();
        }

        return Current;
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(Current, JsonOpts);
        File.WriteAllText(FilePath, json);
    }
}
