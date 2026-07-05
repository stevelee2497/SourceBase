# Desktop — API URL from environment

## Goal

Point Jupiter at the API without hardcoding the URL in C# source and without exposing it as a user setting. The URL is configuration: supplied by the CI pipeline for releases and by a local `.env` for development.

## Behavior

- The API URL is **configuration, not a user setting** — the field is hidden from the Settings window.
- Resolved at runtime by `Config.ApiUrl.Resolve()` from the `API_URL` environment variable. Bare host → `https://` prepended; blank/unset → `null` (API sync no-ops).
- `SettingsStore.Load()` re-resolves the URL after deserializing `settings.json`, so a stale persisted value never overrides config.

## Mechanism (no hardcoded literal)

- `Config.ApiUrl.Resolve()` reads `Environment.GetEnvironmentVariable("API_URL")` and normalizes it.
- `AppSettings.ApiBaseUrl` initializes from `Config.ApiUrl.Resolve()`.
- No MSBuild codegen, no NuGet config packages, no compiled-in constant. The URL literal lives only in pipeline config / a local `.env`, never in tracked C#.

## Local development

- Copy `SourceBase.Desktop/.env.example` → `.env` (git-ignored) with `API_URL=api.domain.com`.
- `Config.DotEnv.Load()` (called first in `App.OnStartup`) reads it into the process env; the csproj copies `.env` next to the exe. No shell exports, no rebuild to change the URL.

## Pipeline

- `desktop-publish.yml` sets `API_URL: ${{ vars.API_URL }}` as an env var on the Publish step; the app reads it at runtime.
- Reuses the existing repo variable `API_URL`, already shared by `api-publish.yml` and `web-publish.yml` — no new variable needed. No `.env` ships in published builds.

## Failure / offline modes

Unchanged: empty/unset URL → API calls no-op; unreachable API → silent failure per existing `HabitLogService` behavior.
