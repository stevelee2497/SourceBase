using H.NotifyIcon;
using SourceBase.Desktop.Overlay;
using SourceBase.Desktop.Scheduling;
using SourceBase.Desktop.Services;
using SourceBase.Desktop.Settings;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace SourceBase.Desktop;

public partial class App : Application
{
    private const string MutexName = "Jupiter.SingleInstance";
    private Mutex? _mutex;

    private readonly SettingsStore _store = new();
    private TaskbarIcon? _tray;
    private RestScheduler? _scheduler;
    private HabitLogService? _habitLogService;
    private OverlayWindow? _activeOverlay;
    private Icon? _trayIcon;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out var isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        _store.Load();
        _habitLogService = new HabitLogService(() => _store.Current, _store.Save);

        _trayIcon = CreateTrayIcon(TrayGlyph.Mug);

        _tray = new TaskbarIcon
        {
            ToolTipText = "Jupiter - rest reminders",
            ContextMenu = (ContextMenu)Resources["TrayMenu"],
            Icon = _trayIcon,
        };
        _tray.TrayMouseDoubleClick += (_, _) => ShowOverlay();
        _tray.ForceCreate();

        _scheduler = new RestScheduler(() => _store.Current);
        _scheduler.DueForRest += (_, _) => ShowOverlay();
        _scheduler.VideoSuppressed += (_, _) => _habitLogService?.LogSuppressedVideo();
        _scheduler.Start();

        _ = Task.Run(async () =>
        {
            await UpdateService.CheckAsync();
            if (UpdateService.PendingUpdate is { } update)
                Dispatcher.Invoke(() =>
                    _tray?.ShowBalloonTip(
                        "SourceBase Update",
                        $"v{update.Version} is ready. Open Settings to install.",
                        BalloonIcon.Info));
        });
    }

    private enum TrayGlyph { Mug, Pause, Leaf, Cup, Droplet }

    // In OnStartup, change the call to:
    //     _trayIcon = CreateTrayIcon(TrayGlyph.Mug);

    /// <summary>Draws a crisp white vector glyph as a 32x32 tray icon (font-independent).</summary>
    private static Icon CreateTrayIcon(TrayGlyph glyph)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var white = new Pen(Color.White, 2.4f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            };
            var fill = Brushes.White;

            switch (glyph)
            {
                case TrayGlyph.Mug:
                    // Beer/coffee mug: body + handle + foam.
                    g.FillRectangle(fill, 8, 12, 12, 14);            // body
                    g.DrawRectangle(white, 8, 12, 12, 14);
                    g.DrawArc(white, 18, 13, 8, 9, -90, 180);        // handle
                    g.FillEllipse(fill, 8, 8, 5, 5);                 // foam
                    g.FillEllipse(fill, 11, 6, 6, 6);
                    g.FillEllipse(fill, 15, 8, 5, 5);
                    break;

                case TrayGlyph.Pause:
                    // Two rounded bars — "take a break".
                    g.FillRectangle(fill, 10, 8, 4, 16);
                    g.FillRectangle(fill, 18, 8, 4, 16);
                    break;

                case TrayGlyph.Leaf:
                    // Leaf — calm/rest.
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddBezier(8, 24, 8, 10, 22, 8, 24, 8);
                        path.AddBezier(24, 8, 24, 22, 10, 24, 8, 24);
                        g.FillPath(fill, path);
                    }
                    g.DrawLine(new Pen(Color.FromArgb(120, 0, 0, 0), 1.2f), 11, 21, 21, 11); // vein notch
                    break;

                case TrayGlyph.Cup:
                    // Tea cup with saucer + steam.
                    g.FillRectangle(fill, 9, 14, 12, 8);             // cup
                    g.DrawArc(white, 19, 14, 7, 7, -90, 180);        // handle
                    g.FillRectangle(fill, 7, 23, 16, 2);             // saucer
                    g.DrawLine(white, 12, 10, 12, 7);                // steam
                    g.DrawLine(white, 16, 10, 16, 7);
                    break;

                case TrayGlyph.Droplet:
                    // Water drop — hydrate.
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddBezier(16, 6, 24, 16, 24, 20, 16, 26);
                        path.AddBezier(16, 26, 8, 20, 8, 16, 16, 6);
                        g.FillPath(fill, path);
                    }
                    break;
            }
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void ShowOverlay()
    {
        if (_activeOverlay is not null) return;

        var overlay = new OverlayWindow(_store.Current);
        _activeOverlay = overlay;

        overlay.Snoozed += (_, _) => { _scheduler?.Snooze(); _habitLogService?.LogSnoozed(); };
        overlay.Dismissed += (_, _) => _habitLogService?.LogDismissed();
        overlay.HabitsStarted += (_, habits) => _habitLogService?.LogHabitsStarted(habits);
        overlay.Closed += (_, _) => _activeOverlay = null;

        overlay.Show();
        overlay.Activate();
    }

    private void OnBreakNow(object sender, RoutedEventArgs e) => ShowOverlay();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_store.Current);
        window.ShowDialog();

        if (window.Saved)
        {
            _store.Save();
            _scheduler?.ScheduleNext();
        }
    }

    private void OnExitClicked(object sender, RoutedEventArgs e) => Shutdown();

    private void OnExit(object sender, ExitEventArgs e)
    {
        _scheduler?.Stop();
        _tray?.Dispose();
        _trayIcon?.Dispose();
        _mutex?.Dispose();
    }
}