using System.Windows.Input;

namespace SourceBase.Desktop.Models;

public enum ApiConnectionStatus { None, Connected, Failed }

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

    /// <summary>Modifier keys for the global "show rest overlay" hotkey. Default Ctrl+Alt.</summary>
    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

    /// <summary>Key for the global "show rest overlay" hotkey. Default L. Null disables the hotkey.</summary>
    public Key? HotkeyKey { get; set; } = Key.L;

    /// <summary>Base URL of the SourceBase API. Resolved from config (build-time API_URL, or the
    /// API_URL env var / .env in local dev) — not user-editable. See <see cref="Config.ApiUrl"/>.</summary>
    public string? ApiBaseUrl { get; set; } = Config.ApiUrl.Resolve();

    /// <summary>Username (email) used to authenticate with the API.</summary>
    public string? ApiUsername { get; set; }

    /// <summary>Password used to authenticate with the API.</summary>
    public string? ApiPassword { get; set; }

    /// <summary>Access token obtained from the last successful login. Set programmatically — do not edit manually.</summary>
    public string? ApiToken { get; set; }

    /// <summary>Refresh token obtained from the last successful login. Set programmatically — do not edit manually.</summary>
    public string? ApiRefreshToken { get; set; }

    /// <summary>Result of the last API call; drives the connected/reconnect indicator in Settings.</summary>
    public ApiConnectionStatus ApiStatus { get; set; } = ApiConnectionStatus.None;

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
        new() { Id = Guid.Empty, Name = "Drink Water",  Emoji = "💧", Accent = "#3B82F6" },
        new() { Id = Guid.Empty, Name = "Push Up",      Emoji = "💪", Accent = "#EF4444" },
        new() { Id = Guid.Empty, Name = "Stretching",   Emoji = "🧘", Accent = "#10B981" },
        new() { Id = Guid.Empty, Name = "Rest Eyes",    Emoji = "👀", Accent = "#8B5CF6" },
        new() { Id = Guid.Empty, Name = "Short Walk",   Emoji = "🚶", Accent = "#F59E0B" },
        new() { Id = Guid.Empty, Name = "Deep Breaths", Emoji = "🌬️", Accent = "#06B6D4" },
    ];
}
