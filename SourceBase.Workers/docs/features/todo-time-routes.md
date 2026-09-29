## Workers port: TodoLists, Todos, TimeSheets

### Goal
- Port the .NET TodoLists, Todos, and TimeSheets endpoint groups into `SourceBase.Workers`.
- Keep endpoint logic self-contained per route file and preserve existing `/api/*` auth + payload behavior where practical.

### Endpoint groups

#### TodoLists
- `POST /api/todo-lists`
  - Body: `{ name: string }`
  - Validation: `name` required, trimmed, max 200 chars
  - Response `200`: `{ id: string }`
- `GET /api/todo-lists`
  - Query: `page?`, `limit?`, `order?`, `orderBy?`
  - Supported `order`: `Asc | Desc`
  - Supported `orderBy`: `Name | CreatedOn | UpdatedOn`
  - Response `200`: `{ items, page, limit, total }`
  - Item shape: `{ id, name, itemCount, createdOn, createdBy, isDefault }`
  - `isDefault` is `true` when list id matches `users.default_todo_list_id`
- `PATCH /api/todo-lists/:id`
  - Partial update semantics: omitted or `null` `name` keeps existing value
  - Validation: route id required and not empty GUID; if `name` present, non-empty and max 200
  - Response `200`: `{ id }`
- `DELETE /api/todo-lists/:id`
  - Validation: route id required and not empty GUID
  - Response `200`: `{ success: true }`

#### Todos
- `POST /api/todos`
  - Body: `{ date: string, title: string, status?: "Open" | "Completed" | "Archived", todoListId?: string | null }`
  - Validation: `date` required ISO date, `title` required non-empty, `status` enum when provided
  - If `todoListId` is provided, it must belong to the current user or return `400`
  - Response `200`: `{ id }`
- `GET /api/todos/:id`
  - Validation: route id required and not empty GUID
  - Response `200`: `{ id, userId, date, title, status, todoListId, createdOn, createdBy, updatedOn, updatedBy }`
- `GET /api/todos`
  - Query filters: `status?`, `date?`, `todoListId?`, `page?`, `limit?`, `order?`, `orderBy?`
  - Supported `orderBy`: `Date | Title | Status | CreatedOn | CreatedBy | UpdatedOn | UpdatedBy`
  - Response `200`: paging envelope matching .NET shape
- `PATCH /api/todos/:id`
  - Partial update semantics: omitted or `null` fields keep existing values
  - Body: `{ date?: string | null, title?: string | null, status?: enum | null }`
  - Validation: route id required and not empty GUID; if `title` present, non-empty
  - Response `200`: `{ id }`
- `DELETE /api/todos/:id`
  - Validation: route id required and not empty GUID
  - Response `200`: `{ success: true }`

#### TimeSheets
- `POST /api/time-sheets`
  - Body: `{ items: Array<{ date: string, project: string, hours: number }> }`
  - Validation:
    - `items` required and non-empty
    - each `project` required non-empty
    - each `hours` must be `> 0` and `<= 8`
    - each `date` required ISO date
  - Upsert key: `(user_id, date, project)`
  - Response `200`: `{ ids: string[] }`
- `GET /api/time-sheets/:id`
  - Validation: route id required and not empty GUID
  - Response `200`: `{ id, date, project, hours, createdOn, createdBy, updatedOn, updatedBy }`
- `GET /api/time-sheets`
  - Query filters: `from?`, `to?`, `year?`, `month?`, `date?`, `page?`, `limit?`, `order?`, `orderBy?`
  - Effective date window matches .NET:
    - `from = from ?? date ?? first day of (year, month)`
    - `to = to ?? date ?? last day of (year, month)`
  - Supported `orderBy`: `Date | Project | Hours | CreatedOn | UpdatedOn`
  - Response `200`: paging envelope
- `DELETE /api/time-sheets/:id`
  - Validation: route id required and not empty GUID
  - Response `200`: `{ success: true }`

### Auth / authorization
- All routes require the existing `requireAuth` middleware.
- Reads and mutations are scoped to the authenticated user.
- Missing/invalid auth returns existing Workers auth error shape/status.
- Cross-user access returns `404` for item/list/time-sheet lookups and deletes.

### Persistence / schema
- Extend Workers schema with:
  - `todo_items`
  - `time_sheets`
  - `notifications` (persistence only; no push delivery)
- Keep audit fields (`created_on`, `created_by`, `updated_on`, `updated_by`) on new tables.
- Add indexes for user/date/filter/order lookups.
- `todo_items.todo_list_id` is nullable and references `todo_lists(id)`.
- `time_sheets` should support efficient upsert-by-query on `(user_id, date, project)`.

### Notable compatibility decisions
- Notification side effects may be persisted as rows only; SignalR/live delivery is out of scope.
- Workers should preserve .NET partial-update semantics where explicit `null` means “leave unchanged”, not “clear”.
