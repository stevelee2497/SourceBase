namespace SourceBase.Desktop.Models;

/// <summary>
/// User-configurable settings, persisted to %AppData%\SourceBase.Desktop\settings.json.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Minutes between rest overlays. Default 30; configurable to 60, etc.</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>Length of the suggested rest, shown to the user. Default 5 minutes.</summary>
    public int RestMinutes { get; set; } = 5;

    /// <summary>Snooze duration when the user defers a break.</summary>
    public int SnoozeMinutes { get; set; } = 5;

    /// <summary>Only fire between these hours (24h). Null = always.</summary>
    public int? WorkingHourStart { get; set; } = 9;
    public int? WorkingHourEnd { get; set; } = 17;

    /// <summary>Launch the app automatically at Windows login.</summary>
    public bool StartAtLogin { get; set; } = false;

    /// <summary>Base URL of the SourceBase API (e.g. https://api.example.com). Leave null to disable logging.</summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>Bearer token for the SourceBase API. Obtain by logging in via the web app.</summary>
    public string? ApiToken { get; set; }

    /// <summary>When true, suppress the overlay while a fullscreen / video app is active.</summary>
    public bool PauseDuringVideo { get; set; } = true;

    /// <summary>Process names (no .exe) that should suppress the overlay when focused.</summary>
    public List<string> BlockedProcesses { get; set; } =
    [
        "stremio",
        "vlc",
        "mpc-hc",
        "mpc-be",
        "wmplayer",
        "netflix",
        "spotify",
    ];

    /// <summary>Days the reminder is active. Defaults to weekdays Mon–Fri.</summary>
    public List<DayOfWeek> ActiveDays { get; set; } =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday,
    ];

    /// <summary>The habits available to pick during a rest.</summary>
    public List<Habit> Habits { get; set; } = DefaultHabits();

    public static List<Habit> DefaultHabits() =>
    [
        new() { Id = "drink-water", Name = "Drink Water",  Emoji = "💧", Accent = "#3B82F6" },
        new() { Id = "push-up",     Name = "Push Up",      Emoji = "💪", Accent = "#EF4444" },
        new() { Id = "stretching",  Name = "Stretching",   Emoji = "🧘", Accent = "#10B981" },
        new() { Id = "eye-rest",    Name = "Rest Eyes",    Emoji = "👀", Accent = "#8B5CF6" },
        new() { Id = "walk",        Name = "Short Walk",   Emoji = "🚶", Accent = "#F59E0B" },
        new() { Id = "deep-breath", Name = "Deep Breaths", Emoji = "🌬️", Accent = "#06B6D4" },
    ];
}
