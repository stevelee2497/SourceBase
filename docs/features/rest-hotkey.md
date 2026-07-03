# Desktop: Global Rest Hotkey

Jupiter (SourceBase.Desktop) lets the user trigger the rest overlay on demand via a
system-wide keyboard shortcut, in addition to the existing tray double-click and
"Take a break now" menu item.

## Behavior

1. Default shortcut is **Ctrl+Alt+L**. It is registered as a Windows global hotkey
   (`RegisterHotKey`/`WM_HOTKEY`) via a hidden message-only window, so it fires even
   when Jupiter has no focused window (it's a tray-only app).
2. Pressing the hotkey calls the same `ShowOverlay()` path as the tray menu — if an
   overlay is already open, the hotkey is a no-op (mirrors existing re-entrancy guard).
3. The combo is user-configurable from Settings → "Rest hotkey": focus the field and
   press a new combination; it must include at least one modifier (Ctrl/Alt/Shift/Win)
   to avoid accidentally capturing plain typing keys.
4. Pressing **Esc** while the hotkey field is focused disables the hotkey
   (`HotkeyKey = null`) — no shortcut is registered, other trigger paths still work.
5. Saving Settings re-registers the hotkey immediately (unregister old combo, register
   new one) — no restart required.

## Settings involved

- `AppSettings.HotkeyModifiers` (`System.Windows.Input.ModifierKeys`, default `Control | Alt`)
- `AppSettings.HotkeyKey` (`System.Windows.Input.Key?`, default `Key.L`, `null` = disabled)

Persisted to `%AppData%\Jupiter\settings.json` like every other setting; no schema
migration needed since a missing/old file just falls back to the field defaults.

## Failure / offline modes

- If `RegisterHotKey` fails (combo already claimed by another app or the OS), Jupiter
  does not crash or show an error dialog — it silently keeps the tray/menu trigger
  working and simply has no global shortcut. Consistent with this app's
  never-block-on-native-failure convention.
- No API/network involvement — this is a purely local UI trigger, no sync impact.

## Out of scope

- No visual "recording…" affordance beyond the live-updating text field while capturing
  a new combo — kept minimal per Phase 1 scope.
