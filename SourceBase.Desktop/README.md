# SourceBase.Desktop

A local Windows tray app that nudges you to rest. On a configurable interval
(default **every 30 minutes**) it shows a dimmed fullscreen overlay with a white
centered modal telling you to step away for ~5 minutes and pick a habit to do —
shown as emoji/image cards.

This is **Phase 1: local only, no API.** Phase 2 syncs with the SourceBase habit
tracker submodule (`api.domain.com`).

## Why a tray app, not a Windows Service

Windows Services run in Session 0 and cannot draw on the interactive desktop, so
they can't show an overlay. The correct pattern (and what BreakTimer does) is a
background tray app that auto-starts at login. That's this project.

## Run

```bash
dotnet run --project SourceBase.Desktop
```

The app starts in the tray. Double-click the tray icon, use "Take a break now",
or press the global rest hotkey (default **Ctrl+Alt+L**, configurable in Settings)
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
- Rest hotkey: Ctrl+Alt+L (rebindable in Settings; disable by pressing Esc while
  the field is focused)
- Default habits: Drink Water 💧, Push Up 💪, Stretching 🧘, Rest Eyes 👀,
  Short Walk 🚶, Deep Breaths 🌬️

Each habit can use an emoji or an `imagePath` (image takes priority).

### API URL (not user-editable)

The API base URL is **configuration, not a user setting** — the field is hidden
from the Settings window. It's resolved at runtime by `Config.ApiUrl`, which
prefers the `API_URL` environment variable (dev override) and otherwise falls
back to a value **embedded into the assembly at publish time** (a bare host is
normalized to `https://`; blank / unset → `null`, which leaves API sync
disabled). No hardcoded literal in tracked source, no config packages.

**Local development** — copy `.env.example` to `.env` (git-ignored) in the
`SourceBase.Desktop/` folder:

```
API_URL=api.domain.com
```

`dotnet run` / `dotnet build` copies `.env` next to the exe; `Config.DotEnv`
loads it into the process environment at startup. No shell exports, no rebuild
needed to change the URL.

**CI** — `desktop-publish.yml` passes the existing GitHub repo variable
`vars.API_URL` (shared with `api-publish.yml` / `web-publish.yml`) to
`dotnet publish` as `/p:ApiUrl=...`; MSBuild embeds it as `[AssemblyMetadata]` so
the value reaches the user's machine at runtime. No `.env` is shipped in
published builds.

## Project layout

```
Models/        Habit, AppSettings (+ default seed)
Settings/      SettingsStore — JSON load/save to %AppData%
Scheduling/    RestScheduler — interval timer + working-hours gate, skips a scheduled
               reminder if a habit was started in the last 20 min
Services/      HabitLogService, StartupService, UpdateService, GlobalHotkeyService (RegisterHotKey)
Overlay/       OverlayWindow — dimmed backdrop, white modal, habit cards, rest countdown
Assets/        jupiter.ico — app icon (Explorer, taskbar, Alt-Tab), set via <ApplicationIcon>
App.xaml(.cs)  Tray icon, single-instance mutex, wiring
```

## Phase 2 (later)

- Settings window (interval/rest/working-hours/habit editor) — replaces the stub dialog
- Multi-monitor overlays (one window per screen)
- "Start at login" registry toggle
- Idle detection (skip reminders when away — `GetLastInputInfo`)
- Write picked habit back to the API via `LogHabitEntry`
- Pull habit list from the API instead of local defaults
