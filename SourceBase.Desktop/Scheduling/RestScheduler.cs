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
    private readonly DispatcherTimer _timer;
    private readonly Func<AppSettings> _settings;
    private DateTime _nextDue;

    public event EventHandler? DueForRest;

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
        var minutes = overrideMinutes ?? _settings().IntervalMinutes;
        _nextDue = DateTime.Now.AddMinutes(Math.Max(1, minutes));
    }

    public void Snooze() => ScheduleNext(_settings().SnoozeMinutes);

    private void OnTick(object? sender, EventArgs e)
    {
        if (DateTime.Now < _nextDue) return;

        var s = _settings();
        if (!IsActiveDay(s) || !WithinWorkingHours(s)) { ScheduleNext(); return; }

        if (s.PauseDuringVideo && PresentationDetector.ShouldSuppress(s.BlockedProcesses))
        {
            // Don't reset the full interval — retry shortly so the reminder
            // fires soon after the video ends, not a full interval later.
            _nextDue = DateTime.Now.AddMinutes(2);
            return;
        }

        ScheduleNext();
        DueForRest?.Invoke(this, EventArgs.Empty);
    }

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
