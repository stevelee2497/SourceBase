## Users, Roles, and Data endpoint port

### Scope

Port only these .NET endpoint groups into `SourceBase.Workers`:

- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`
- `POST /api/users/{id}/confirm-email`
- `POST /api/users/{id}/reset-password`
- `GET /api/roles`
- `POST /api/roles`
- `PATCH /api/roles/{id}`
- `DELETE /api/roles/{id}`
- `POST /api/data/enums`
- `GET /api/data/stats`

### Authorization

- Admin-only: every users endpoint except auth routes, plus `POST/PATCH/DELETE /api/roles`
- Anonymous: `GET /api/roles`, `POST /api/data/enums`
- Authenticated: `GET /api/data/stats`

### Request and response compatibility

- Keep .NET route shapes and camelCase JSON payloads.
- Users create/update should preserve .NET duplicate and role-existence messages where practical:
  - `Username is already taken.`
  - `Email is already taken.`
  - `One or more specified roles do not exist.`
- Roles create/update/delete should preserve:
  - `Role name is already taken.`
  - `Admin role cannot be updated.`
  - `Role not found or cannot delete Admin role.`
- Validation stays compatible with existing Worker conventions:
  - zod-driven 400 validation response body
  - email/password/name length checks
- List endpoints return paging payloads:
  - `{ items, page, limit, total }`

### Behavioral rules

- `CreateUser`
  - creates an unconfirmed user with OTP, hashed password, and new security stamp
  - inserts confirmation email row
  - if a notifications table exists in Workers, insert a notification for each admin user
  - optional roles are normalized by trim + case-insensitive distinct before validation
- `UpdateUser`
  - requires `email`
  - updates scalar fields directly, allowing nullable fields to clear values
  - if email changes:
    - set `emailConfirmed = false`
    - generate new OTP
    - generate new security stamp
    - insert confirmation email row
  - if roles change, generate new security stamp
  - `roles: null` or omitted means no role change
  - `roles: []` also leaves roles unchanged to match current .NET logic
- `ConfirmUserEmail`
  - sets `emailConfirmed = true`
  - clears OTP fields
- `ResetUserPassword`
  - replaces password hash
  - rotates security stamp
  - inserts reset email row containing the admin-provided password
- `DeleteUser`
  - deletes the user and returns `{ success: true }`
- `GetEnums`
  - supports only `RolesOrder`, `TodoItemStatus`, and `Roles`
- `GetStats`
  - counts all users
  - computes current-user wallet balance from initial balances and signed transactions
  - computes current-month income/expense
  - computes weekly timesheet completion string like `3/3 days this week`

### Schema impact

Add only the minimal Workers schema required for this slice:

- `notifications`
- `wallets`
- `transactions`
- `time_sheets`

These support `CreateUser` notification inserts and `GetStats` queries.

### Tests

Add focused Vitest coverage for:

- anonymous roles and enums access
- admin-only protection for users/roles
- admin role CRUD guardrails
- admin user management flow
- authenticated stats behavior
