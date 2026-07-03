using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Registers a system-wide hotkey (via <c>RegisterHotKey</c>/<c>WM_HOTKEY</c> on a hidden
/// message-only window) that fires <see cref="Pressed"/> regardless of which app has focus —
/// Jupiter is a tray-only app with no window of its own to receive key events.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x4A50; // "JP"
    private const int WM_HOTKEY = 0x0312;

    private readonly HwndSource _source;
    private readonly Func<AppSettings> _settings;
    private bool _registered;

    public event EventHandler? Pressed;

    public GlobalHotkeyService(Func<AppSettings> settings)
    {
        _settings = settings;
        _source = new HwndSource(new HwndSourceParameters("JupiterHotkeyWindow") { WindowStyle = 0 });
        _source.AddHook(WndProc);
        Register();
    }

    /// <summary>Unregisters the previous combo (if any) and registers the current settings' combo.</summary>
    public void Register()
    {
        Unregister();

        var s = _settings();
        if (s.HotkeyKey is null) return;

        try
        {
            var vk = KeyInterop.VirtualKeyFromKey(s.HotkeyKey.Value);
            _registered = RegisterHotKey(_source.Handle, HotkeyId, (uint)s.HotkeyModifiers, (uint)vk);
        }
        catch
        {
            // Combo already claimed by another app, or the OS rejected it — the tray
            // menu / double-click trigger still works, so this must never crash Jupiter.
            _registered = false;
        }
    }

    public void Unregister()
    {
        if (_registered) UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY || wParam.ToInt32() != HotkeyId) return IntPtr.Zero;

        handled = true;
        Pressed?.Invoke(this, EventArgs.Empty);
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
