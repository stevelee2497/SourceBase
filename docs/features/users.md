# User Management Features

Admin-only endpoints for managing application users. All endpoints are under `/api/users` and require the `Admin` role.

---

## Create User

**Endpoint:** `POST /api/users`
**Auth:** Admin only

### Use Case

As an admin, I want to create user accounts on behalf of others, so that I can onboard new users without requiring them to self-register.

### Description

1. Admin sends `userName`, `email`, `password`, optional `firstName`, `lastName`, `phoneNumber`, and an optional list of `roles`.
2. If the username or email is already taken → `400 Bad Request`.
3. If any specified role does not exist in the database → `400 Bad Request`.
4. Role names are normalised (trimmed, case-insensitive de-duplicated) before assignment.
5. A new user is created with a hashed password, an OTP confirmation code, and the requested roles.
6. A confirmation email is sent to the new user.
7. Returns the new user's `Id`.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-CREATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-CREATE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-CREATE-003 | Valid data creates user and returns 200 | ✅ Pass |
| USERS-CREATE-004 | Unknown role name returns 400 Bad Request | ✅ Pass |
| USERS-CREATE-005 | Mix of valid and invalid roles returns 400 Bad Request | ✅ Pass |
| USERS-CREATE-006 | Duplicate email (case-insensitive) returns 400 Bad Request | ✅ Pass |
| USERS-CREATE-007 | Roles with surrounding whitespace are normalised and de-duplicated | ✅ Pass |

---

## Get Users

**Endpoint:** `GET /api/users`
**Auth:** Admin only

### Use Case

As an admin, I want to list all registered users with paging and ordering, so that I can manage and audit user accounts.

### Description

1. Admin sends optional paging parameters (`page`, `limit`, `order`, `orderBy`).
2. Returns a paginated list of users with their profile fields and roles.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-GET-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-GET-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-GET-003 | Admin user receives paginated list including newly created users | ✅ Pass |
| USERS-GET-004 | Paging and ordering parameters return the correct page | ✅ Pass |

---

## Update User

**Endpoint:** `PUT /api/users/{id}`
**Auth:** Admin only

### Use Case

As an admin, I want to update a user's profile and role assignments, so that I can correct information or adjust permissions.

### Description

1. Admin sends the target user `id` (route) plus updated `userName`, `email`, optional profile fields, and `roles`.
2. If the email is changed, `EmailConfirmed` is set to `false`, a new OTP code is issued, and a re-confirmation email is sent.
3. If the roles list is changed, the user's security stamp is rotated, invalidating their existing tokens.
4. Duplicate email (case-insensitive) → `400 Bad Request`. Unknown role → `400 Bad Request`.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-UPDATE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-UPDATE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-UPDATE-003 | Valid update returns 200 | ✅ Pass |
| USERS-UPDATE-004 | Unknown role name returns 400 Bad Request | ✅ Pass |
| USERS-UPDATE-005 | Duplicate email (case-insensitive) returns 400 Bad Request | ✅ Pass |
| USERS-UPDATE-006 | Email change sets EmailConfirmed=false and issues new OTP | ✅ Pass |
| USERS-UPDATE-007 | Role change invalidates the user's existing access token | ✅ Pass |

---

## Delete User

**Endpoint:** `DELETE /api/users/{id}`
**Auth:** Admin only

### Use Case

As an admin, I want to delete a user account, so that I can remove deactivated or unwanted accounts from the system.

### Description

1. Admin provides the target user `id` (route).
2. If the user doesn't exist → `404 Not Found`.
3. The user record is deleted from the database.
4. Any existing tokens issued to that user are implicitly invalidated because the user no longer exists.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-DELETE-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-DELETE-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-DELETE-003 | Existing user is deleted and returns 200 | ✅ Pass |
| USERS-DELETE-004 | Unknown user ID returns 404 Not Found | ✅ Pass |
| USERS-DELETE-005 | After deletion, the user's existing token is rejected | ✅ Pass |

---

## Confirm User Email

**Endpoint:** `POST /api/users/{id}/confirmEmail`
**Auth:** Admin only

### Use Case

As an admin, I want to manually confirm a user's email, so that I can unblock accounts without requiring the user to go through the email verification flow.

### Description

1. Admin provides the target user `id` (route).
2. If the user doesn't exist → `404 Not Found`.
3. `EmailConfirmed` is set to `true` on the user record.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-CONFIRM-EMAIL-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-CONFIRM-EMAIL-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-CONFIRM-EMAIL-003 | Non-existent user returns 404 Not Found | ✅ Pass |
| USERS-CONFIRM-EMAIL-004 | Valid user returns 200 | ✅ Pass |
| USERS-CONFIRM-EMAIL-005 | After confirmation, user's EmailConfirmed flag is true | ✅ Pass |

---

## Reset User Password

**Endpoint:** `POST /api/users/{id}/resetPassword`
**Auth:** Admin only

### Use Case

As an admin, I want to reset a user's password to a new random value and notify them by email, so that I can help users who are locked out of their accounts.

### Description

1. Admin provides the target user `id` (route) and a `newPassword`.
2. If the user doesn't exist → `404 Not Found`.
3. The password must meet the minimum length requirement (6 characters) → `400 Bad Request` otherwise.
4. The user's password is updated and the security stamp is rotated.
5. An email is sent to the user notifying them of their new password.

### Test Cases

| Test Case ID | Description | Status |
|---|---|---|
| USERS-RESET-PWD-001 | Missing token returns 401 Unauthorized | ✅ Pass |
| USERS-RESET-PWD-002 | Non-admin user returns 403 Forbidden | ✅ Pass |
| USERS-RESET-PWD-003 | Non-existent user returns 404 Not Found | ✅ Pass |
| USERS-RESET-PWD-004 | Valid request returns 200 | ✅ Pass |
| USERS-RESET-PWD-005 | Reset triggers email to the user with the new password | ✅ Pass |
| USERS-RESET-PWD-006 | Password shorter than 6 characters returns 400 Bad Request | ✅ Pass |
