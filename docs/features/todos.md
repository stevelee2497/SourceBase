# Todo Features

Endpoints for managing todo lists and todo items. All endpoints require authentication. Todo lists and items are scoped to the authenticated user — users cannot see or modify each other's data.

Todo Lists endpoints are under `/api/todo-lists`. Todo Items endpoints are under `/api/todos`.

---

## Create Todo List

**Endpoint:** `POST /api/todo-lists`
**Auth:** Required

### Use Case

As an authenticated user, I want to create a named todo list, so that I can organise my tasks into logical groups.

### Description

1. Client sends `name` (required, max 200 characters).
2. The todo list is created and associated with the authenticated user's ID.
3. Returns the new list's `Id`.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOLISTS-CREATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOLISTS-CREATE-002 | Valid name creates list and returns 200 | ✅ Pass |
| TODOLISTS-CREATE-003 | Missing name returns 400 Bad Request | ✅ Pass |
| TODOLISTS-CREATE-004 | Created list is owned by the authenticated user | ✅ Pass |

---

## Get Todo Lists

**Endpoint:** `GET /api/todo-lists`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve my todo lists, so that I can navigate to and manage my task collections.

### Description

1. Client calls the endpoint with a valid access token.
2. Returns only the todo lists that belong to the authenticated user — other users' lists are never included.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOLISTS-GET-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOLISTS-GET-002 | Authenticated user receives their lists and returns 200 | ✅ Pass |
| TODOLISTS-GET-003 | Only the current user's lists are returned, not other users' | ✅ Pass |

---

## Update Todo List

**Endpoint:** `PUT /api/todo-lists/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to rename one of my todo lists, so that I can keep its name relevant to my tasks.

### Description

1. Client sends the list `id` (route) and a new `name`.
2. If the list doesn't exist or belongs to a different user → `404 Not Found`.
3. The list name is updated and the updated list `Id` is returned.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOLISTS-UPDATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOLISTS-UPDATE-002 | Valid update returns 200 | ✅ Pass |
| TODOLISTS-UPDATE-003 | Updating another user's list returns 404 Not Found | ✅ Pass |

---

## Delete Todo List

**Endpoint:** `DELETE /api/todo-lists/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete one of my todo lists, so that I can remove collections I no longer need.

### Description

1. Client provides the list `id` (route).
2. If the list doesn't exist or belongs to a different user → `404 Not Found`.
3. The list is deleted from the database.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOLISTS-DELETE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOLISTS-DELETE-002 | Existing owned list is deleted and returns 200 | ✅ Pass |
| TODOLISTS-DELETE-003 | Deleting another user's list returns 404 Not Found | ✅ Pass |

---

## Create Todo

**Endpoint:** `POST /api/todos`
**Auth:** Required

### Use Case

As an authenticated user, I want to create a todo item with a title, date, and status, so that I can track individual tasks.

### Description

1. Client sends `title` (required), `date` (required), `status`, and an optional `todoListId`.
2. If `todoListId` is provided but doesn't exist or doesn't belong to the current user → `404 Not Found`.
3. The todo item is created and associated with the authenticated user and optionally a todo list.
4. Returns the new item's `Id`.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOS-CREATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOS-CREATE-002 | Valid data creates todo and returns 200 | ✅ Pass |
| TODOS-CREATE-003 | Created todo's `CreatedBy` is set to the authenticated user's username | ✅ Pass |
| TODOS-CREATE-004 | Missing title returns 400 Bad Request | ✅ Pass |
| TODOS-CREATE-005 | Missing date returns 400 Bad Request | ✅ Pass |
| TODOS-CREATE-006 | Valid todoListId creates todo linked to the list | ✅ Pass |
| TODOS-CREATE-007 | Invalid or another user's todoListId returns 404 Not Found | ✅ Pass |

---

## Get Todos

**Endpoint:** `GET /api/todos`
**Auth:** Required

### Use Case

As an authenticated user, I want to list my todo items with filtering, paging, and ordering, so that I can view and navigate my tasks efficiently.

### Description

1. Client sends optional filters: `status`, `date`, `todoListId`, plus paging parameters.
2. Returns only todo items belonging to the authenticated user.
3. Filters are applied as AND conditions — only items matching all provided filters are returned.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOS-GET-ALL-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOS-GET-ALL-002 | Authenticated user receives their todos and returns 200 | ✅ Pass |
| TODOS-GET-ALL-003 | Only the current user's todos are returned, not other users' | ✅ Pass |
| TODOS-GET-ALL-004 | Status and date filters return only matching items | ✅ Pass |
| TODOS-GET-ALL-005 | Paging and ordering parameters return the correct page | ✅ Pass |
| TODOS-GET-ALL-006 | Filtering by todoListId returns only items in that list | ✅ Pass |

---

## Get Todo

**Endpoint:** `GET /api/todos/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the details of a specific todo item, so that I can view its full information.

### Description

1. Client provides the todo `id` (route).
2. If the item doesn't exist or belongs to a different user → `404 Not Found`.
3. Returns the full todo item details.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOS-GET-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOS-GET-002 | Correct data is returned after item creation | ✅ Pass |
| TODOS-GET-003 | Non-existent ID returns 404 Not Found | ✅ Pass |
| TODOS-GET-004 | Requesting another user's todo returns 404 Not Found | ✅ Pass |

---

## Update Todo

**Endpoint:** `PUT /api/todos/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to update a todo item's title, date, and status, so that I can keep my task list current.

### Description

1. Client sends the todo `id` (route) plus updated `title`, `date`, and `status`.
2. If the item doesn't exist or belongs to a different user → `404 Not Found`.
3. The item fields are updated.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOS-UPDATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOS-UPDATE-002 | Valid update returns 200 | ✅ Pass |
| TODOS-UPDATE-003 | Missing title returns 400 Bad Request | ✅ Pass |
| TODOS-UPDATE-004 | Non-existent todo ID returns 404 Not Found | ✅ Pass |
| TODOS-UPDATE-005 | Updating another user's todo returns 404 Not Found | ✅ Pass |

---

## Delete Todo

**Endpoint:** `DELETE /api/todos/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete a todo item, so that I can remove tasks I no longer need to track.

### Description

1. Client provides the todo `id` (route).
2. If the item doesn't exist or belongs to a different user → `404 Not Found`.
3. The item is removed from the database.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| TODOS-DELETE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| TODOS-DELETE-002 | Existing owned todo is deleted and returns 200 | ✅ Pass |
| TODOS-DELETE-003 | Non-existent ID returns 404 Not Found | ✅ Pass |
| TODOS-DELETE-004 | Deleting another user's todo returns 404 Not Found | ✅ Pass |
