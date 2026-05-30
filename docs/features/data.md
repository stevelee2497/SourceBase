# Data & Utility Features

Read-only utility endpoints for observability, enum definitions, and application statistics. All endpoints are under `/api/data`.

---

## Get Audits

**Endpoint:** `GET /api/data/audits`
**Auth:** Admin only

### Use Case

As an admin, I want to view the audit history of all data changes in the system, so that I can trace who changed what and when for compliance and debugging.

### Description

1. Admin sends optional paging parameters (`page`, `limit`, `order`).
2. Results are sorted by `ActionOn` (most recent first by default).
3. Each entry includes `author`, `action`, `entityType`, `entityId`, and JSON snapshots of the `current`, `original`, and `changes` state.
4. Audit records are written automatically by `ApplicationDbContextAuditInterceptor` on every save — this endpoint only reads them.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| DATA-AUDITS-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| DATA-AUDITS-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| DATA-AUDITS-003 | Admin receives audit history including recent changes | ✅ Pass |
| DATA-AUDITS-004 | Results are ordered most-recent-first by default | ✅ Pass |

---

## Get Enums

**Endpoint:** `POST /api/data/enums`
**Auth:** Anonymous

### Use Case

As a client application, I want to fetch the definitions of one or more enum types in a single request, so that I can populate dropdowns and labels without hard-coding values.

### Description

1. Client sends a list of `enums` (e.g. `["TodoItemStatus", "Roles"]`).
2. The list must not be empty → `400 Bad Request` if empty.
3. Static enum types (`RolesOrder`, `TodoItemStatus`) are resolved from the .NET enum values.
4. The special `Roles` enum type is resolved dynamically from the database, returning the current list of roles.
5. Returns a dictionary keyed by enum type, each containing a list of `{ name, description }` entries.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| DATA-ENUMS-001 | Requesting static enum types returns only those definitions | ✅ Pass |
| DATA-ENUMS-002 | Requesting the Roles enum returns current roles from the database | ✅ Pass |
| DATA-ENUMS-003 | Empty enums list returns 400 Bad Request | ✅ Pass |

---

## Get Stats

**Endpoint:** `GET /api/data/stats`
**Auth:** Required

### Use Case

As an authenticated user, I want to view application-wide statistics, so that I can get a quick overview of users, tasks, and completion rates.

### Description

1. Client calls the endpoint with a valid access token.
2. Returns four aggregate counts from the database:
   - `userCount` — total number of registered users
   - `totalTodoLists` — total number of todo lists
   - `totalTodoItems` — total number of todo items
   - `completedTodoItems` — count of items with status `Completed`

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| DATA-STATS-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| DATA-STATS-002 | Authenticated user receives stats object and returns 200 | ✅ Pass |
| DATA-STATS-003 | `completedTodoItems` never exceeds `totalTodoItems` | ✅ Pass |
