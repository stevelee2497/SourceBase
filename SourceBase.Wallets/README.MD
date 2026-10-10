# Jupiter Wallet

A standalone website for managing wallets, transactions, transfers and transaction categories (with icon upload). It's the wallet part of SourceBase, split out so it can live in its own repository.

- **Frontend** (`frontend/`): React 19 + TypeScript + Tailwind CSS v4 (Vite), hosted on Cloudflare Pages. The look matches the original Blazor app. The top bar has two icons: Wallets (home) and Categories.
- **Backend** (`backend/`): TypeScript Cloudflare Worker (Hono), with D1 (SQLite) for data and R2 for uploaded icon images.
- **Auth**: Auth0 (SPA login, JWT verified by the Worker). Data is scoped by the Auth0 `sub`.
- **Cost**: Cloudflare free tier plus Auth0 free plan.

The guiding rule is to keep everything simple; see `.claude/skills/jupiter-wallet/SKILL.md`.

## Structure

```
backend/    Hono Worker: src/features/<resource>.ts, migrations/, test/
frontend/   React SPA: src/pages/, src/components/, src/lib/
docs/       api.md, setup-and-deploy.md, requirements.md
```

## Quick start

```bash
npm --prefix SourceBase.Wallets/backend ci
npm --prefix SourceBase.Wallets/frontend ci

npm --prefix SourceBase.Wallets/backend test        # 180+ API tests, no accounts needed
npm --prefix SourceBase.Wallets/frontend test
```

To run it for real (Auth0 tenant, D1 database, R2 bucket) and to deploy to Cloudflare, follow **[docs/setup-and-deploy.md](docs/setup-and-deploy.md)**.

## Docs

- [docs/setup-and-deploy.md](docs/setup-and-deploy.md): step-by-step setup, local run and Cloudflare deployment.
- [docs/api.md](docs/api.md): endpoints and how they differ from the .NET API.
- [docs/requirements.md](docs/requirements.md): the original migration brief.
