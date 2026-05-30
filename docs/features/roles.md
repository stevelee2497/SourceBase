# Role Management Features

Endpoints for managing application roles. All write operations require the `Admin` role. The `Get Roles` endpoint is public. All endpoints are under `/api/roles`.

---

## Create Role

**Endpoint:** `POST /api/roles`
**Auth:** Admin only

### Use Case

As an admin, I want to create a new role, so that I can define custom permission levels and assign them to users.

### Description

1. Admin sends `name` and `description`.
2. Role names are compared case-insensitively — if a role with the same name already exists → `400 Bad Request`.
3. The name is trimmed of surrounding whitespace before persisting.
4. A new role is saved and its `Id` is returned.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| ROLES-CREATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| ROLES-CREATE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| ROLES-CREATE-003 | Valid data creates role and returns 200 | ✅ Pass |
| ROLES-CREATE-004 | Duplicate role name (case-insensitive) returns 400 Bad Request | ✅ Pass |
| ROLES-CREATE-005 | Role name with surrounding whitespace is trimmed before persisting | ✅ Pass |

---

## Get Roles

**Endpoint:** `GET /api/roles`
**Auth:** Anonymous

### Use Case

As any client (authenticated or not), I want to retrieve the list of available roles with paging and ordering, so that I can populate dropdowns and role-assignment UIs.

### Description

1. Client sends optional paging parameters (`page`, `limit`, `order`, `orderBy`).
2. Returns a paginated list of roles (`id`, `name`, `description`).

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| ROLES-GET-001 | Anonymous client receives seeded roles | ✅ Pass |
| ROLES-GET-002 | Newly created role appears in the list | ✅ Pass |
| ROLES-GET-003 | Paging and ordering parameters return the correct page | ✅ Pass |

---

## Update Role

**Endpoint:** `PUT /api/roles/{id}`
**Auth:** Admin only

### Use Case

As an admin, I want to update a role's name and description, so that I can keep role definitions accurate.

### Description

1. Admin sends the role `id` (route) plus updated `name` and `description`.
2. The `Admin` role is protected and cannot be renamed or modified → `400 Bad Request`.
3. If the new name is already used by a different role → `400 Bad Request`.
4. Updating a role to its current name (no-op rename) is valid and returns 200.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| ROLES-UPDATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| ROLES-UPDATE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| ROLES-UPDATE-003 | Valid update returns 200 | ✅ Pass |
| ROLES-UPDATE-004 | Duplicate name (case-insensitive, different role) returns 400 Bad Request | ✅ Pass |
| ROLES-UPDATE-005 | Attempting to update the Admin role returns 400 Bad Request | ✅ Pass |
| ROLES-UPDATE-006 | Updating a role with its own current name returns 200 | ✅ Pass |

---

## Delete Role

**Endpoint:** `DELETE /api/roles/{id}`
**Auth:** Admin only

### Use Case

As an admin, I want to delete a role that is no longer needed, so that I can keep the role list clean and accurate.

### Description

1. Admin provides the role `id` (route).
2. If the role doesn't exist → `400 Bad Request`.
3. The `Admin` role is protected and cannot be deleted → `400 Bad Request`.
4. The role is removed from the database.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| ROLES-DELETE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| ROLES-DELETE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| ROLES-DELETE-003 | Existing non-admin role is deleted and returns 200 | ✅ Pass |
| ROLES-DELETE-004 | Attempting to delete the Admin role returns 400 Bad Request | ✅ Pass |
| ROLES-DELETE-005 | Unknown role ID returns 400 Bad Request | ✅ Pass |
