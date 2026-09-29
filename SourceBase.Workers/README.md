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
