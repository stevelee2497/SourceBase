using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SourceBase.Desktop.Scheduling;

/// <summary>
/// Decides whether a rest overlay should be suppressed because the user is
/// watching video / presenting / gaming. Combines the Windows notification
/// state (covers fullscreen video and games) with a process-name check
/// (covers windowed players like Stremio or a browser playing YouTube).
/// </summary>
public static class PresentationDetector
{
    public static bool ShouldSuppress(IEnumerable<string> blockedProcesses)
    {
        if (IsWindowsBusy()) return true;
        if (IsFullscreenForeground()) return true;
        if (IsBlockedProcessForeground(blockedProcesses)) return true;
        return false;
    }

    // --- Windows "do not disturb" notification state ---------------------

    private enum QueryUserNotificationState
    {
        NotPresent = 1,
        Busy = 2,            // a full-screen app is running (game/video)
        RunningD3dFullScreen = 3,
        PresentationMode = 4, // presentation mode is on
        AcceptsNotifications = 5,
        QuietTime = 6,
        App = 7,
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out QueryUserNotificationState state);

    private static bool IsWindowsBusy()
    {
        try
        {
            if (SHQueryUserNotificationState(out var state) != 0) return false;
            return state is QueryUserNotificationState.Busy
                or QueryUserNotificationState.RunningD3dFullScreen
                or QueryUserNotificationState.PresentationMode
                or QueryUserNotificationState.QuietTime;
        }
        catch
        {
            return false;
        }
    }

    // --- Fullscreen foreground window (covers borderless fullscreen) -----

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    private static bool IsFullscreenForeground()
    {
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;
            if (fg == GetDesktopWindow() || fg == GetShellWindow()) return false;
            if (!GetWindowRect(fg, out var r)) return false;

            var w = r.Right - r.Left;
            var h = r.Bottom - r.Top;

            // Compare against the full virtual screen (handles multi-monitor).
            var screenW = (int)SystemParametersWidth();
            var screenH = (int)SystemParametersHeight();
            return w >= screenW && h >= screenH;
        }
        catch
        {
            return false;
        }
    }

    private static double SystemParametersWidth() =>
        System.Windows.SystemParameters.PrimaryScreenWidth;

    private static double SystemParametersHeight() =>
        System.Windows.SystemParameters.PrimaryScreenHeight;

    // --- Blocked process is the foreground app ---------------------------

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    private static bool IsBlockedProcessForeground(IEnumerable<string> blocked)
    {
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            GetWindowThreadProcessId(fg, out var pid);
            if (pid == 0) return false;

            using var proc = Process.GetProcessById(pid);
            var name = proc.ProcessName; // e.g. "stremio", "vlc", "chrome"

            foreach (var b in blocked)
                if (name.Contains(b, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}
