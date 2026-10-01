# SourceBase.Workers

Initial Cloudflare Workers rewrite for SourceBase.

## Current scope

- Hono-based Worker mounted under `/api`
- SQLite/D1-compatible schema bootstrap
- Auth, categories, wallets, transactions, transfers, notifications, machines, gold prices, and avatar upload-url endpoints ported from the .NET API
- Vitest coverage for auth plus focused feature endpoint contracts

## Commands

```sh
npm install
npm run typecheck
npm test
npm run dev
```

## Environment

Copy `.dev.vars.example` to `.dev.vars` for local `wrangler dev`.

Required secrets:

- `JWT_SECRET`
- `REFRESH_TOKEN_SECRET`

Useful vars:

- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`
- `ACCESS_TOKEN_EXPIRES_IN`
- `REFRESH_TOKEN_EXPIRES_IN`
- `OTP_TOKEN_EXPIRATION_MINUTES`
- `WEB_URL`
- `ALLOWED_ORIGINS`
- `REDIS_AVAILABLE`
- `R2_BUCKET_NAME`
- `R2_ACCOUNT_ID`
- `R2_PUBLIC_URL`
- `R2_UPLOAD_EXPIRES_IN`

Required for avatar upload signing:

- `R2_ACCESS_KEY_ID`
- `R2_SECRET_ACCESS_KEY`

## Notes

- Endpoint files are intentionally self-contained: each route keeps its schema, auth handling, query logic, and audit updates nearby.
- Some duplication is acceptable in this project so each endpoint can be ported independently from the current .NET slice.
- Browser clients need CORS configured; for local web development set `ALLOWED_ORIGINS=http://localhost:3001`.

## Production deployment

The repository now includes `.github/workflows/workers-publish.yml` for Cloudflare Workers deploys.

Before that workflow can succeed:

1. Create a D1 database in Cloudflare and replace `database_id` in `wrangler.jsonc`.
2. Configure Worker runtime secrets in Cloudflare:
   - `JWT_SECRET`
   - `REFRESH_TOKEN_SECRET`
   - `ADMIN_PASSWORD`
   - `R2_SECRET_ACCESS_KEY` when avatar upload signing is enabled
3. Configure Worker runtime vars in Cloudflare:
   - `ADMIN_EMAIL`
   - `ACCESS_TOKEN_EXPIRES_IN`
   - `REFRESH_TOKEN_EXPIRES_IN`
   - `OTP_TOKEN_EXPIRATION_MINUTES`
   - `WEB_URL`
   - `ALLOWED_ORIGINS`
   - `REDIS_AVAILABLE`
   - `R2_ACCESS_KEY_ID`
   - `R2_BUCKET_NAME`
   - `R2_ACCOUNT_ID`
   - `R2_PUBLIC_URL`
   - `R2_UPLOAD_EXPIRES_IN`
4. Add GitHub deployment secrets:
   - `CF_API_TOKEN`
   - `CF_ACCOUNT_ID`

The workflow runs `npm ci`, `npm run typecheck`, `npm test`, applies remote D1 migrations, and then deploys the Worker.
