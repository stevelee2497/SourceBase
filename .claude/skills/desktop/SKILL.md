---
name: desktop
description: 'Windows WPF tray-app architecture and conventions for SourceBase.Desktop (Jupiter). Use when writing or reviewing tray icon wiring, overlay windows, schedulers, settings persistence, or API sync code in SourceBase.Desktop.'
trigger: /desktop
---

# /desktop

Desktop app (WPF tray app "Jupiter") coding conventions and architecture for SourceBase.

## Superpowers Workflow

This skill operates under the [Superpowers](https://github.com/obra/Superpowers) methodology. Before writing desktop code, follow these conventions — they are mandatory workflows, not suggestions.

### Before coding (spec-first)

- **Write the spec to /docs/features first** for any non-trivial feature (e.g. docs/features/<feature-name>.md): behavior, settings involved, failure/offline modes, and any API sync impact. This is the source of truth the implementation is reviewed against.
- **Brainstorm before building.** Tease out the UX first — what triggers it, what the user sees, how it degrades when the API is unreachable — before touching XAML or C#.
- **YAGNI.** This is Phase 1 (local-only, no MVVM framework). Don't introduce a ViewModel layer, DI container, or MediatR-style pipeline unless the task genuinely needs it — match the existing code-behind + plain-class style.

### While coding (TDD where testable)

- UI/XAML code-behind (`OverlayWindow`, `SettingsWindow`, `App`) is not unit-tested — verify it manually via `dotnet run`.
- Logic that doesn't touch WPF (`RestScheduler`'s slot math, `PresentationDetector`, JSON round-tripping in `SettingsStore`) should be covered by tests in `SourceBase.Tests` where practical — write the failing test first.
- Test scheduling/time logic by injecting `Func<AppSettings>` / `Func<DateTime>` rather than reading `DateTime.Now` directly inside the test.

### After coding (verify, don't claim)

- **Evidence over claims.** Run `dotnet build` for `SourceBase.Desktop` and confirm green. For behavior changes, run `dotnet run --project SourceBase.Desktop` and exercise the tray icon / overlay / settings window manually — this project has no automated UI test coverage.
- **Update the docs/features spec** and `SourceBase.Desktop/README.md` when settings, project layout, or Phase 2 scope changes.

### Principle summary

- Spec-first · Manual verification for UI, TDD for pure logic · YAGNI · Evidence over claims.

## Commands

```sh
# Development
dotnet run --project SourceBase.Desktop

# Build
dotnet build SourceBase.Desktop

# Publish a portable exe (matches desktop-publish.yml)
dotnet publish SourceBase.Desktop -c Release -p:PublishSingleFile=true
```

## Architecture

Single WPF project, no Clean-Architecture split — it's a standalone client that talks to the SourceBase API over HTTP, it does not reference `SourceBase.Application`/`SourceBase.Domain`.

```
SourceBase.Desktop/
  Models/        Habit, AppSettings (+ default seed) — plain POCOs, no EF
  Settings/      SettingsStore (JSON load/save to %AppData%), SettingsWindow (code-behind)
  Scheduling/    RestScheduler (DispatcherTimer + working-hours gate), PresentationDetector
  Overlay/       OverlayWindow — dimmed backdrop, habit cards, rest countdown
  Services/      HabitLogService (API sync), StartupService (registry), UpdateService (GitHub releases)
  App.xaml(.cs)  Tray icon, single-instance mutex, wiring
```

Data flow: `App` owns every long-lived object (`SettingsStore`, `RestScheduler`, `HabitLogService`, the tray icon) and wires them together with C# events — there is no DI container. `RestScheduler.DueForRest` triggers `ShowOverlay()`; `OverlayWindow` raises `Snoozed`/`Dismissed`/`HabitsStarted`, which `App` forwards into `HabitLogService` and `RestScheduler.Snooze()`.

```csharp
public sealed class RestScheduler
{
    private readonly DispatcherTimer _timer;
    private readonly Func<AppSettings> _settings;

    public event EventHandler? DueForRest;

    public RestScheduler(Func<AppSettings> settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _timer.Tick += OnTick;
    }

    public void Start() { ScheduleNext(); _timer.Start(); }
}
```

## Key rules & conventions

- **Settings are read live via `Func<AppSettings>`**, not captured by value — components always see the current `_store.Current`, so settings changes apply without re-wiring.
- **Persistence**: `SettingsStore` reads/writes `%AppData%\Jupiter\settings.json` with `JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`. Corrupt file → fall back to `new AppSettings()` rather than crashing on startup. Never let an empty `Habits` list survive a load — reseed with `AppSettings.DefaultHabits()`.
- **API calls are fire-and-forget and silently no-op** when `ApiBaseUrl`/`ApiToken` are unset or the request fails — a desktop tray app must never crash or block the UI because the API is unreachable. On `401 Unauthorized`, call `TryRefreshAsync` once and retry; on repeated failure, set `ApiConnectionStatus.Failed` and call `onSave()` so the Settings window can surface it.
- **Cross-component communication uses C# events** (`EventHandler`, `EventHandler<T>`), not callbacks passed through constructors beyond simple `Func`/`Action` accessors (e.g. `HabitLogService(Func<AppSettings> settings, Action onSave)`).
- **Long-running/native work is static classes with P/Invoke or `Registry`** (`StartupService`, `PresentationDetector`), not injected services — there's no DI container in this project.
- **Sealed by default.** Concrete classes (`SettingsStore`, `RestScheduler`, `HabitLogService`) are `sealed`; only `App`/`Window` subclasses are non-sealed (WPF requires `partial`).
- **XAML code-behind, no MVVM.** Wire event handlers directly in the constructor (`StartButton.Click += (_, _) => StartRest();`) rather than introducing `ICommand`/binding infrastructure — consistent with Phase 1 scope.
- **App identity**: assembly/product name is `Jupiter` (`AssemblyName`, `Product` in the `.csproj`) — the WPF project namespace stays `SourceBase.Desktop`. Don't let the two drift when adding new files.
- **Config** — add new user-facing settings to `SourceBase.Desktop/Models/AppSettings.cs` with an XML-doc comment explaining default/units; wire the corresponding control into `SettingsWindow`. This is a separate `AppSettings` from `SourceBase.Application`'s — don't conflate them.
- **Versioning/publish**: `desktop-publish.yml` tags releases as `desktop-v1.<n>` and renames the published exe to `Jupiter-v<version>.exe`; `UpdateService` polls GitHub releases with that same `desktop-v`/`Jupiter-v` prefix convention — keep them in sync if either changes.
- **Silent failure on background work**: prefer `try { ... } catch { return null/false; }` over surfacing exceptions for anything network- or registry-related — this app runs unattended in the tray and must never show a crash dialog for a transient failure.
