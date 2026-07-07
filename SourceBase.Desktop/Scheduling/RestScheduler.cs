using System.Windows.Threading;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Scheduling;

/// <summary>
/// Drives the rest reminder. Ticks once a minute, and raises <see cref="DueForRest"/>
/// when the configured interval has elapsed since the last rest, provided the current
/// time falls inside working hours.
/// </summary>
public sealed class RestScheduler
{
    private const int HabitStartedDebounceMinutes = 20;

    private readonly DispatcherTimer _timer;
    private readonly Func<AppSettings> _settings;
    private DateTime _nextDue;
    private bool _videoSuppressedLogged;

    public event EventHandler? DueForRest;

    /// <summary>Fires once per video/game session when a due reminder is suppressed.</summary>
    public event EventHandler? VideoSuppressed;

    public DateTime NextDue => _nextDue;

    public RestScheduler(Func<AppSettings> settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        ScheduleNext();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    /// <summary>Reset the countdown — call after settings change or after a rest is shown/snoozed.</summary>
    public void ScheduleNext(int? overrideMinutes = null)
    {
        if (overrideMinutes is not null)
        {
            // Snooze: relative delay from now.
            _nextDue = DateTime.Now.AddMinutes(Math.Max(1, overrideMinutes.Value));
            return;
        }
        _nextDue = NextAlignedSlot(DateTime.Now, _settings());
    }

    // Snaps to the next N-minute slot anchored at WorkingHourStart (or midnight if unset).
    // e.g. start=9:00, interval=30 → slots are 9:00, 9:30, 10:00, 10:30 …
    private static DateTime NextAlignedSlot(DateTime now, AppSettings s)
    {
        var interval = Math.Max(1, s.IntervalMinutes);
        var anchor = now.Date.AddHours(s.WorkingHourStart ?? 0);
        if (now < anchor) return anchor;
        var elapsed = (long)((now - anchor).TotalMinutes / interval);
        return anchor.AddMinutes((elapsed + 1) * interval);
    }

    public void Snooze() => ScheduleNext(_settings().SnoozeMinutes);

    private void OnTick(object? sender, EventArgs e)
    {
        if (DateTime.Now < _nextDue) return;

        var s = _settings();
        if (!IsActiveDay(s) || !WithinWorkingHours(s)) { ScheduleNext(); return; }

        if (s.PauseDuringVideo && PresentationDetector.ShouldSuppress(s.BlockedProcesses))
        {
            if (!_videoSuppressedLogged)
            {
                _videoSuppressedLogged = true;
                VideoSuppressed?.Invoke(this, EventArgs.Empty);
            }
            // Don't reset the full interval — retry shortly so the reminder
            // fires soon after the video ends, not a full interval later.
            _nextDue = DateTime.Now.AddMinutes(2);
            return;
        }
        _videoSuppressedLogged = false;

        ScheduleNext();
        if (RecentlyStartedHabit(s)) return;
        DueForRest?.Invoke(this, EventArgs.Empty);
    }

    // A habit started just before the reminder came due means the user already took their
    // break on their own — firing the overlay again this soon would just be nagging.
    private static bool RecentlyStartedHabit(AppSettings s) =>
        s.LastHabitStartedAt is { } last && DateTime.Now - last < TimeSpan.FromMinutes(HabitStartedDebounceMinutes);

    private static bool IsActiveDay(AppSettings s) =>
        s.ActiveDays.Count == 0 || s.ActiveDays.Contains(DateTime.Now.DayOfWeek);

    private static bool WithinWorkingHours(AppSettings s)
    {
        if (s.WorkingHourStart is null || s.WorkingHourEnd is null) return true;
        var hour = DateTime.Now.Hour;
        var start = s.WorkingHourStart.Value;
        var end = s.WorkingHourEnd.Value;
        return start <= end
            ? hour >= start && hour < end
            : hour >= start || hour < end; // wraps past midnight
    }
}
