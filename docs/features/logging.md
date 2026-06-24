# Logging

## Problem

Logs stored only on the VPS become inaccessible when the server goes down under a bot attack.
This document describes the two-layer strategy that ensures logs are always readable.

## Architecture

```
API / EmailWorker
  └─► Serilog pipeline
        ├─► Console (JSON via CompactJsonFormatter)   — stdout, visible in Dozzle
        ├─► File (async, CLEF format, daily rolling)  — local disk backup
        └─► Betterstack                               — external SaaS, survives VPS outage ✓
```

**Key property**: Betterstack receives log events via HTTPS as they are written.
Even if the VPS crashes seconds later, all events up to that point are in Betterstack.

## Betterstack Logtail

- Dashboard: <https://logs.betterstack.com>
- Free tier: 1 GB/month · 3-day retention
- Real-time log tail, full-text search, structured field filters
- Serilog package: `BetterStack.Logs.Serilog`

### How to access logs when the VPS is down

1. Open <https://logs.betterstack.com>
2. Select your source (e.g. `sourcebase-api`)
3. Use the live tail or search for `RateLimitExceeded` events to see attacker IPs

## Rate-limit logging

Every `429 Too Many Requests` response is logged as a `Warning` with structured fields:

| Field         | Description                          |
| ------------- | ------------------------------------ |
| `ClientIp`    | Attacker IP (from `X-Forwarded-For`) |
| `RequestPath` | Endpoint being hammered              |
| `UserAgent`   | Bot user agent string                |
| `RetryAfter`  | Seconds until the window resets      |

Example Betterstack query to find top attacking IPs:

```
level:Warning ClientIp:* | group by ClientIp
```

## File sink

- Path: `Logs/log-.clef` (inside container, mounted to `./logs/api` on host)
- Format: CLEF (Compact Log Event Format) — readable with `clef` CLI or Seq
- Rolling: daily, no auto-delete

## Async wrapper

The `File` sink is wrapped in `Serilog.Sinks.Async` so disk I/O never blocks the API
thread pool during high-load attacks. The Betterstack sink is already async/batching
by nature (periodic batching sink).

## Environment variables

| Variable                 | Where                      | Source        |
| ------------------------ | -------------------------- | ------------- |
| `BetterStackSourceToken` | API container, EmailWorker | GitHub secret |

### GitHub Actions secret

Add `BETTERSTACK_SOURCE_TOKEN` as a **Repository Secret** in:
`Settings → Secrets and variables → Actions → New repository secret`

Get the token from Betterstack: `Sources → your source → Source token`

## Adding a new service

To ship logs from a new service to Betterstack:

1. Add `BetterStack.Logs.Serilog` to the project (version in `Directory.Packages.props`).
2. In the Serilog setup, read `configuration["BetterStackSourceToken"]` and call
   `logConfig.WriteTo.BetterStack(token)` when non-empty.
3. Add `- BetterStackSourceToken=${BETTERSTACK_SOURCE_TOKEN}` to the service's env in
   `docker-compose.yml`.
4. Add the secret variable to the `envs` list in `.github/workflows/docker-publish.yml`.
