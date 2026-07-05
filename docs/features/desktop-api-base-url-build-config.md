# Desktop — Build-time default ApiBaseUrl

## Goal

Ship Jupiter with a sensible default `ApiBaseUrl` so a fresh install talks to the production API without the user hand-editing `settings.json` — **without hardcoding the URL literal in C# source**. The value is injected at build time from a GitHub Actions environment variable.

## Behavior

- The API URL is **configuration, not a user setting** — the field is hidden from the Settings window.
- Resolved at runtime by `Config.ApiUrl.Resolve()`, priority: `API_URL` env var → `BuildConfig.ApiBaseUrl` (build-time). Bare host → `https://` prepended; blank → `null` (API sync no-ops).
- `SettingsStore.Load()` re-resolves the URL after deserializing `settings.json`, so a stale persisted value never overrides config.

## Mechanism (no hardcoded literal)

- MSBuild reads the `API_URL` environment variable into an MSBuild property with an empty fallback.
- A `BuildConfig.g.cs` is generated at build time (one MSBuild item per line for clean output) containing `public const string ApiBaseUrl = "<value>";`.
- `AppSettings.ApiBaseUrl` initializes from `Config.ApiUrl.Resolve()`.
- The URL literal lives only in pipeline config / a local `.env`, never in tracked C#.

## Local development

- Copy `SourceBase.Desktop/.env.example` → `.env` (git-ignored) with `API_URL=api.domain.com`.
- `Config.DotEnv.Load()` (called first in `App.OnStartup`) reads it; the csproj copies `.env` next to the exe. No shell exports, no rebuild to change the URL.

## Pipeline

- `desktop-publish.yml` sets `API_URL: ${{ vars.API_URL }}` as an env var on the Publish step.
- Reuses the existing repo variable `API_URL` (`api.domain.com`), already shared by `api-publish.yml` and `web-publish.yml` — no new variable needed. No `.env` ships in published builds.

## Failure / offline modes

Unchanged: empty/unset URL → API calls no-op; unreachable API → silent failure per existing `HabitLogService` behavior.
