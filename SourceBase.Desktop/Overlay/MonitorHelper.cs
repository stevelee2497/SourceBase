using System.Runtime.InteropServices;
using System.Windows;

namespace SourceBase.Desktop.Overlay;

/// <summary>
/// Enumerates connected monitors so the rest overlay can cover every screen, not just the
/// primary one. Bounds are expressed in the same DIP space as <see cref="SystemParameters"/>
/// (scaled by the primary monitor's DPI) since that's the coordinate space WPF expects for
/// <see cref="Window"/> placement before the window has been shown.
/// </summary>
public static class MonitorHelper
{
    public readonly record struct MonitorBounds(double Left, double Top, double Width, double Height, bool IsPrimary);

    public static List<MonitorBounds> GetAllMonitors()
    {
        var monitors = new List<MonitorBounds>();
        try
        {
            var primaryScale = GetDpiForMonitor(MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY)) / 96.0;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref Rect rect, IntPtr _) =>
            {
                var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(hMonitor, ref info)) return true;

                monitors.Add(new MonitorBounds(
                    info.rcMonitor.Left / primaryScale,
                    info.rcMonitor.Top / primaryScale,
                    (info.rcMonitor.Right - info.rcMonitor.Left) / primaryScale,
                    (info.rcMonitor.Bottom - info.rcMonitor.Top) / primaryScale,
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0));
                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            monitors.Clear();
        }

        if (monitors.Count == 0)
            monitors.Add(new MonitorBounds(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight, true));

        return monitors;
    }

    private static uint GetDpiForMonitor(IntPtr hMonitor)
    {
        try
        {
            return GetDpiForMonitor(hMonitor, 0, out var dpiX, out _) == 0 ? dpiX : 96;
        }
        catch
        {
            return 96;
        }
    }

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;
    private const uint MONITORINFOF_PRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
