# SourceBase.Workers — foundation and auth slice

## Goal

Create an initial `SourceBase.Workers/` Cloudflare Workers service that preserves the current `/api` contract style while starting the rewrite with the auth surface and the core runtime foundation.

## Scope in this slice

- Create a new TypeScript Workers workspace.
- Use **Hono** for routing and **Zod** for request validation.
- Use **SQLite/D1-compatible SQL** for persistence.
- Keep endpoint code **one file per endpoint**, with the route, schema, auth checks, query logic, audit updates, and response mapping kept close together.
- Implement the current auth endpoints:
  - `POST /api/auth/register`
  - `POST /api/auth/confirmEmail`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `POST /api/auth/logout`
  - `GET /api/auth/info`
  - `PATCH /api/auth/info`
  - `POST /api/auth/forgotPassword`
  - `POST /api/auth/resendConfirmationEmail`
  - `POST /api/auth/resetPassword`

## Out of scope for this slice

- Full parity for the remaining 71 non-auth endpoints.
- SignalR / realtime notifications.
- Gold price background jobs.
- Redis-backed caching and redis-status behavior.
- R2 upload endpoints.
- Cloudflare deployment secrets and production D1 data migration.

## Contract rules

- Routes stay under `/api`.
- Request/response JSON remains camelCase.
- Incoming strings are trimmed before validation to match the existing .NET JSON converter behavior.
- Authenticated endpoints reject requests when:
  - the bearer token is missing or invalid,
  - the user no longer exists,
  - the email is not confirmed,
  - or the persisted `securityStamp` no longer matches the token claim.
- Error responses use:
  - `traceId`
  - `code`
  - `message`
  - optional `errors`

## Auth design for Workers

- Access tokens and refresh tokens are Worker-signed JWTs.
- Both token types include:
  - `sub` = user id
  - `email`
  - `userName`
  - `roles`
  - `securityStamp`
- `logout` and `resetPassword` rotate `securityStamp`, invalidating both access and refresh tokens.
- `confirmEmail` assigns the default `User` role.

## Initial SQLite/D1 schema in this slice

- `users`
- `roles`
- `user_roles`
- `emails`
- `todo_lists` (only to support `defaultTodoListId` in auth info updates)

## Validation rules

- `register`
  - `userName`: required, max 256
  - `email`: required, valid email
  - `password`: required, min 6
- `confirmEmail`
  - `email`: required, valid email
  - `code`: required, length 6
- `login`
  - `email`: required, valid email
  - `password`: required
- `refresh`
  - `token`: required
- `forgotPassword`
  - `email`: required, valid email
- `resendConfirmationEmail`
  - `email`: required, valid email
- `resetPassword`
  - `email`: required, valid email
  - `code`: required, length 6
  - `newPassword`: required, min 6
- `updateUserInfo`
  - partial-update semantics
  - `firstName`: max 100 when provided
  - `lastName`: max 100 when provided
  - `phoneNumber`: max 20 when provided
  - `null` means "keep existing value", not "clear"

## Failure modes to preserve

- `register` duplicate username/email → `400`
- `confirmEmail` unknown user or invalid/expired code → `401`
- `login` unknown user / unconfirmed user / wrong password → `401`
- `refresh` invalid token / rotated security stamp → `401`
- `logout` without valid auth → `401`
- `forgotPassword` unknown user → `404`
- `resendConfirmationEmail` unknown user → `404`
- `resendConfirmationEmail` for already-confirmed user → `400`
- `resetPassword` unknown user → `404`
- `resetPassword` invalid or expired code → `400`

## Test strategy

- Use `Vitest` with an in-memory SQLite-backed fake D1 adapter.
- Seed admin/user-role data in tests so auth flows can be validated without Cloudflare infrastructure.
- Cover the main contract-critical flows:
  - register + confirm + login
  - refresh token rotation rules
  - logout invalidation
  - get/update auth info
  - forgot/reset password
