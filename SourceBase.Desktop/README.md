# SourceBase.Desktop

A local Windows tray app that nudges you to rest. On a configurable interval
(default **every 30 minutes**) it shows a dimmed fullscreen overlay with a white
centered modal telling you to step away for ~5 minutes and pick a habit to do —
shown as emoji/image cards.

This is **Phase 1: local only, no API.** Phase 2 syncs with the SourceBase habit
tracker submodule (`api.quoctran.qzz.io`).

## Why a tray app, not a Windows Service

Windows Services run in Session 0 and cannot draw on the interactive desktop, so
they can't show an overlay. The correct pattern (and what BreakTimer does) is a
background tray app that auto-starts at login. That's this project.

## Run

```bash
dotnet run --project SourceBase.Desktop
```

The app starts in the tray. Double-click the tray icon (or "Take a break now")
to preview the overlay immediately without waiting for the interval.

## Build a portable exe

```bash
dotnet publish SourceBase.Desktop -c Release -p:PublishSingleFile=true
```

## Settings

Stored at `%AppData%\SourceBase.Desktop\settings.json`. Defaults:

- Interval: 30 min (set to 60 for hourly, etc.)
- Rest length: 5 min
- Working hours: 09:00–22:00
- Default habits: Drink Water 💧, Push Up 💪, Stretching 🧘, Rest Eyes 👀,
  Short Walk 🚶, Deep Breaths 🌬️

Each habit can use an emoji or an `imagePath` (image takes priority).

## Project layout

```
Models/        Habit, AppSettings (+ default seed)
Settings/      SettingsStore — JSON load/save to %AppData%
Scheduling/    RestScheduler — interval timer + working-hours gate
Overlay/       OverlayWindow — dimmed backdrop, white modal, habit cards, rest countdown
App.xaml(.cs)  Tray icon, single-instance mutex, wiring
```

## Phase 2 (later)

- Settings window (interval/rest/working-hours/habit editor) — replaces the stub dialog
- Multi-monitor overlays (one window per screen)
- "Start at login" registry toggle
- Idle detection (skip reminders when away — `GetLastInputInfo`)
- Write picked habit back to the API via `LogHabitEntry`
- Pull habit list from the API instead of local defaults
