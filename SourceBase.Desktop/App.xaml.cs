using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using SourceBase.Desktop.Overlay;
using SourceBase.Desktop.Scheduling;
using SourceBase.Desktop.Settings;

namespace SourceBase.Desktop;

public partial class App : Application
{
    private const string MutexName = "SourceBase.Desktop.SingleInstance";
    private Mutex? _mutex;

    private readonly SettingsStore _store = new();
    private TaskbarIcon? _tray;
    private RestScheduler? _scheduler;
    private OverlayWindow? _activeOverlay;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out var isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        _store.Load();

        _tray = new TaskbarIcon
        {
            ToolTipText = "SourceBase — rest reminders",
            ContextMenu = (ContextMenu)Resources["TrayMenu"],
            // Icon: ship an .ico in Assets and set IconSource here.
        };
        _tray.TrayMouseDoubleClick += (_, _) => ShowOverlay();
        _tray.ForceCreate();

        _scheduler = new RestScheduler(() => _store.Current);
        _scheduler.DueForRest += (_, _) => ShowOverlay();
        _scheduler.Start();
    }

    private void ShowOverlay()
    {
        if (_activeOverlay is not null) return; // don't stack overlays

        var overlay = new OverlayWindow(_store.Current);
        _activeOverlay = overlay;

        overlay.Snoozed += (_, _) => _scheduler?.Snooze();
        overlay.HabitPicked += (_, habit) =>
        {
            // Phase 1: local only. Phase 2: POST to LogHabitEntry on the SourceBase API.
        };
        overlay.Closed += (_, _) => _activeOverlay = null;

        overlay.Show();
        overlay.Activate();
    }

    private void OnBreakNow(object sender, RoutedEventArgs e) => ShowOverlay();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        // Phase 1 stub. SettingsWindow comes next — interval/rest/habit editor.
        MessageBox.Show(
            $"Interval: every {_store.Current.IntervalMinutes} min\n" +
            $"Rest: {_store.Current.RestMinutes} min\n" +
            $"Habits: {_store.Current.Habits.Count}",
            "SourceBase — Settings (preview)");
    }

    private void OnExitClicked(object sender, RoutedEventArgs e) => Shutdown();

    private void OnExit(object sender, ExitEventArgs e)
    {
        _scheduler?.Stop();
        _tray?.Dispose();
        _mutex?.Dispose();
    }
}
