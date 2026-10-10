---
name: jupiter-wallet
description: "Conventions for the standalone Jupiter Wallet module (SourceBase.Wallets): React + Tailwind frontend on Cloudflare Pages, Hono + D1 + R2 backend on Cloudflare Workers, Auth0. Use when writing or reviewing code, tests or docs under SourceBase.Wallets."
trigger: /jupiter-wallet
---

# /jupiter-wallet

Standalone wallet and transaction manager. `frontend/` is a Vite + React + TypeScript SPA (Cloudflare Pages). `backend/` is a Hono Worker on D1 (SQLite) and R2, with Auth0 for login. It's independent of the .NET system and will move to its own repository.

## The Ponytail rule: keep it simple

Every decision is checked against this. When two options work, pick the one with less code, fewer files and fewer moving parts.

- No abstraction until the third use. Duplicating a few lines beats a premature helper.
- No new dependency without a clear reason. Prefer the platform (`fetch`, `Intl`, `crypto.randomUUID`, D1 `batch`) over a library.
- Flat structure. One file per backend resource, one folder per page.
- Raw SQL, plain `useState` / `useEffect`, plain `fetch`. No ORM, no state or data-fetching library, no DI container.
- No speculative features, config flags or "future" hooks. Delete dead code instead of commenting it out.
- A short function that does one thing, with guard clauses and early returns instead of nested `if`.
- Constants over magic strings. Comments only for the non-obvious "why".

## Layout

```
backend/   src/index.ts (app + middleware)   src/lib/ (auth, errors, validate, db, json)
           src/features/<resource>.ts        migrations/NNNN_name.sql   test/features/<resource>/
frontend/  src/lib/ (api, format, dateRanges, toasts, theme, types)
           src/components/ (shared UI)       src/pages/<page>/
docs/      requirements.md  api.md  setup-and-deploy.md
```

## Backend conventions

- One file per resource in `src/features/` exporting a Hono router: zod schema, SQL and handlers together. Mounted in `src/index.ts`; everything under `/api` goes through `requireAuth` except `GET /api/icons/files/:key`.
- **Owner scoping on every query**: filter by `user_id = c.get("userId")` (the Auth0 `sub`). A row that isn't yours is a 404, never a 403. Exceptions: system rows (`is_system = 1`) are visible to all but immutable (403).
- **Errors**: throw `notFound()`, `forbidden()`, `badRequest()`, `fieldError(field, msg)` from `lib/errors.ts`. The body shape is `{ traceId, code, message, errors }`. Don't invent new shapes.
- **Validation**: `parseBody` / `parseQuery` with zod. Strings are trimmed, query keys listed in `arrayKeys` accept repeats. The nil GUID in a route id is a 400 (`idParam`).
- **Partial updates** use null-coalescing: `body.name ?? existing.name`.
- **Multi-row writes use `DB.batch([...])`** so they are atomic (transfers, wallet delete). Never two separate `run` calls for one logical change.
- JSON is camelCase with nulls omitted (`ok(c, data)`); columns are snake_case. Dates are `yyyy-MM-dd` text. Enums are strings (`Income`, `Expense`, ...).
- Schema changes are new numbered files in `migrations/`; never edit an applied migration.
- Config goes in `wrangler.jsonc` vars (and `env.d.ts`), secrets in `.dev.vars` / `wrangler secret`. Never commit secrets.

## Frontend conventions

- Reuse the shared classes in `src/index.css` (`.card`, `.btn-primary|secondary|danger`, `.btn-sm`, `.form-input`, `.form-label`, `.badge-*`) and the shared components. Match the original Blazor look exactly: Tailwind v4 defaults, indigo primary, gray neutrals, dark mode via the `dark` class.
- Render every icon through `<Icon value=... />` (emoji, `<path…/>` SVG fragment, or `http` image URL). Never print an icon value as text.
- Call the API only through `useApi()` (`lib/api.ts`): it attaches the Auth0 token and shows error toasts. Add one method per endpoint there.
- Mobile first. Check layouts at 320px and 375px; keep the original `sm` / `md` / `lg` breakpoints.
- Amounts: `formatAmount` (no decimals, thousands separators). Dates: `dd MMM` in lists.
- No sidebar. The top bar has two icons: Wallets (home) and Categories.
- Only `VITE_*` variables reach the browser. None of them is a secret.

## Testing

- Backend: vitest in the Workers runtime (`@cloudflare/vitest-plugin`) against in-memory D1 and R2. `npm --prefix SourceBase.Wallets/backend test`.
- Every test creates its own user with `uniqueUser()` and signs a token with `api(user, ...)`, so tests are independent. Payloads are inline in the test; use the arrange helpers in `test/helpers.ts` (`createWallet`, `createTransaction`, ...) only for setup.
- Test names keep the original .NET ids (`WALLETS-CREATE-001: ...`). One test file per endpoint under `test/features/<resource>/`.
- Every behaviour change or bug fix gets a test. Cover 401, validation 400, ownership 404 and the happy path.
- Test data is always synthetic. Never use real personal or health data.
- Frontend: vitest + Testing Library for pure helpers and small components. `npm --prefix SourceBase.Wallets/frontend test`.

## Before finishing

```
npm --prefix SourceBase.Wallets/backend run typecheck && npm --prefix SourceBase.Wallets/backend test
npm --prefix SourceBase.Wallets/frontend run build && npm --prefix SourceBase.Wallets/frontend test
```

## Commands and setup

See `docs/setup-and-deploy.md` (Auth0, D1, R2, Pages, CI) and `docs/api.md` (endpoint contract).
