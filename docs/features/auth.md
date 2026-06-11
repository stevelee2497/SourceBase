# Auth Features

Authentication and identity management endpoints. All endpoints are under `/api/auth`.

---

## Login

**Endpoint:** `POST /api/auth/login`
**Auth:** Anonymous

### Use Case

As a registered user, I want to log in with my email and password, so that I can receive an access token and refresh token to make authenticated requests.

### Description

1. Client sends `email` and `password`.
2. The server looks up the user by email.
3. If the user doesn't exist, the email is not confirmed, or the password is wrong → `401 Unauthorized`.
4. On success, the JWT middleware issues an access token (JWT) and a refresh token in the response.
5. The response includes `expiresIn` (seconds), which reflects the configured `AccessTokenExpirationMinutes` in `AppSettings`.

### Test Cases

| Test Case ID | Description                                                                                | Status  |
| ------------ | ------------------------------------------------------------------------------------------ | ------- |
| LOGIN-001    | Valid credentials return 200 and access token                                              | ✅ Pass |
| LOGIN-002    | Wrong password returns 401 Unauthorized                                                    | ✅ Pass |
| LOGIN-003    | Unknown email returns 401 Unauthorized                                                     | ✅ Pass |
| LOGIN-004    | Unconfirmed email returns 401 Unauthorized                                                 | ✅ Pass |
| LOGIN-005    | Login succeeds after email is confirmed                                                    | ✅ Pass |
| LOGIN-006    | Missing password field returns 400 Bad Request                                             | ✅ Pass |
| LOGIN-007    | `expiresIn` in the response matches the configured access token lifetime (60 min → 3600 s) | ✅ Pass |

---

## Register

**Endpoint:** `POST /api/auth/register`
**Auth:** Anonymous

### Use Case

As a new user, I want to register an account with my username, email, and password, so that I can access the application after confirming my email.

### Description

1. Client sends `userName`, `email`, and `password`.
2. Username and email are trimmed of surrounding whitespace before processing.
3. If the username or email is already taken → `400 Bad Request`.
4. A new user is created with a hashed password and a 6-digit OTP confirmation code.
5. A confirmation email containing the OTP code is sent to the user's email address.
6. Returns the new user's `Id`.

### Test Cases

| Test Case ID | Description                                                   | Status  |
| ------------ | ------------------------------------------------------------- | ------- |
| REGISTER-001 | Valid data returns 200 and new user ID                        | ✅ Pass |
| REGISTER-002 | Whitespace around email/username is trimmed before validation | ✅ Pass |
| REGISTER-003 | Whitespace around password is trimmed before hashing          | ✅ Pass |
| REGISTER-004 | Duplicate email (case-insensitive) returns 400 Bad Request    | ✅ Pass |
| REGISTER-005 | Invalid email format returns 400 Bad Request                  | ✅ Pass |
| REGISTER-006 | Password shorter than 6 characters returns 400 Bad Request    | ✅ Pass |

---

## Confirm Email

**Endpoint:** `POST /api/auth/confirmEmail`
**Auth:** Anonymous

### Use Case

As a newly registered user, I want to confirm my email with the OTP code I received, so that I can unlock login access to my account.

### Description

1. Client sends `email` and `code` (6-character OTP).
2. The server looks up the user by email — if not found → `401 Unauthorized`.
3. The OTP code is validated against the stored code and its expiry timestamp.
4. If invalid or expired → `401 Unauthorized`.
5. On success, `EmailConfirmed` is set to `true` and the default `User` role is assigned to the account.

### Test Cases

| Test Case ID      | Description                                                        | Status  |
| ----------------- | ------------------------------------------------------------------ | ------- |
| CONFIRM-EMAIL-001 | Valid OTP code confirms email and returns 200                      | ✅ Pass |
| CONFIRM-EMAIL-002 | Invalid OTP code returns 401 Unauthorized                          | ✅ Pass |
| CONFIRM-EMAIL-003 | Expired OTP code returns 401 Unauthorized                          | ✅ Pass |
| CONFIRM-EMAIL-004 | Unknown email returns 401 Unauthorized                             | ✅ Pass |
| CONFIRM-EMAIL-005 | Invalid payload (missing/malformed fields) returns 400 Bad Request | ✅ Pass |

---

## Forgot Password

**Endpoint:** `POST /api/auth/forgotPassword`
**Auth:** Anonymous

### Use Case

As a user who forgot their password, I want to request a password reset code by email, so that I can set a new password.

### Description

1. Client sends their registered `email`.
2. If the user is not found → `404 Not Found`.
3. A new 6-digit OTP code is generated and stored on the user record with an expiry timestamp.
4. An email containing the reset code is sent to the user.
5. Returns `{ success: true }`.

### Test Cases

| Test Case ID   | Description                                               | Status  |
| -------------- | --------------------------------------------------------- | ------- |
| FORGOT-PWD-001 | Valid registered email triggers OTP email and returns 200 | ✅ Pass |
| FORGOT-PWD-002 | Unknown email returns 404 Not Found                       | ✅ Pass |
| FORGOT-PWD-003 | Invalid email format returns 400 Bad Request              | ✅ Pass |

---

## Reset Password

**Endpoint:** `POST /api/auth/resetPassword`
**Auth:** Anonymous

### Use Case

As a user, I want to reset my password using the OTP code I received by email, so that I can regain access to my account.

### Description

1. Client sends `email`, `code` (OTP), and `newPassword`.
2. The server looks up the user by email — if not found → `404 Not Found`.
3. The OTP code is validated against the stored code and its expiry timestamp — if invalid or expired → `400 Bad Request`.
4. On success, the password is updated, the OTP fields are cleared, `EmailConfirmed` is set to `true` (so users who never confirmed their email regain access), and the security stamp is rotated (invalidating all existing tokens).

### Test Cases

| Test Case ID  | Description                                                                                        | Status  |
| ------------- | -------------------------------------------------------------------------------------------------- | ------- |
| RESET-PWD-001 | Valid OTP and new password returns 200                                                             | ✅ Pass |
| RESET-PWD-002 | After reset, user can log in with the new password                                                 | ✅ Pass |
| RESET-PWD-003 | Invalid OTP code returns 400 Bad Request                                                           | ✅ Pass |
| RESET-PWD-004 | Expired OTP code returns 400 Bad Request                                                           | ✅ Pass |
| RESET-PWD-005 | Unknown email returns 404 Not Found                                                                | ✅ Pass |
| RESET-PWD-006 | User who never confirmed email can reset password and log in — email is confirmed as a side-effect | ✅ Pass |

---

## Resend Confirmation Email

**Endpoint:** `POST /api/auth/resendConfirmationEmail`
**Auth:** Anonymous

### Use Case

As a user whose confirmation email expired or was lost, I want to request a new confirmation code, so that I can complete my email verification.

### Description

1. Client sends their registered `email`.
2. If the user is not found → `404 Not Found`.
3. If the email is already confirmed → `400 Bad Request`.
4. A new OTP code is generated and stored with a fresh expiry timestamp.
5. A new confirmation email is sent to the user.

### Test Cases

| Test Case ID    | Description                                         | Status  |
| --------------- | --------------------------------------------------- | ------- |
| RESEND-CONF-001 | Valid unconfirmed email resends OTP and returns 200 | ✅ Pass |
| RESEND-CONF-002 | Already-confirmed email returns 400 Bad Request     | ✅ Pass |
| RESEND-CONF-003 | Unknown email returns 404 Not Found                 | ✅ Pass |
| RESEND-CONF-004 | Invalid email format returns 400 Bad Request        | ✅ Pass |

---

## Logout

**Endpoint:** `POST /api/auth/logout`
**Auth:** Required

### Use Case

As an authenticated user, I want to log out, so that my access and refresh tokens are immediately invalidated on the server.

### Description

1. Client calls the endpoint with a valid access token.
2. The server loads the current user and rotates their security stamp (a new `Guid`).
3. Any previously issued tokens that embed the old security stamp are rejected on next use.
4. Returns `{ success: true }`.

### Test Cases

| Test Case ID | Description                                | Status  |
| ------------ | ------------------------------------------ | ------- |
| LOGOUT-001   | Valid token returns 200                    | ✅ Pass |
| LOGOUT-002   | Missing token returns 401 Unauthorized     | ✅ Pass |
| LOGOUT-003   | After logout, refresh token is invalidated | ✅ Pass |

---

## Refresh Token

**Endpoint:** `POST /api/auth/refresh`
**Auth:** Anonymous

### Use Case

As an authenticated user whose access token has expired, I want to exchange my refresh token for a new access token, so that I can continue using the app without re-entering my credentials.

### Description

1. Client sends the `token` (refresh token string).
2. The server parses the refresh token and extracts `userId` and `securityStamp`.
3. The user is loaded from the database; if not found → `401 Unauthorized`.
4. The stored security stamp is compared with the one in the token — mismatch → `401 Unauthorized` (covers logged-out or password-changed scenarios).
5. On success, a new access token (and refresh token) are issued via the JWT middleware.

### Test Cases

| Test Case ID | Description                                                   | Status  |
| ------------ | ------------------------------------------------------------- | ------- |
| REFRESH-001  | Valid refresh token returns new access token preserving roles | ✅ Pass |
| REFRESH-002  | Invalid/tampered refresh token returns 401 Unauthorized       | ✅ Pass |
| REFRESH-003  | Refresh token used after logout returns 401 Unauthorized      | ✅ Pass |
| REFRESH-004  | Missing token field returns 400 Bad Request                   | ✅ Pass |
| REFRESH-005  | Expired refresh token returns 401 Unauthorized                | ✅ Pass |
| REFRESH-006  | After refreshing, old refresh token returns 401 Unauthorized  | ✅ Pass |

---

## Get User Info

**Endpoint:** `GET /api/auth/info`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve my profile information, so that I can display or use my account details in the application.

### Description

1. Client calls the endpoint with a valid access token.
2. The server reads the current user ID from the JWT claims and loads the user from the database.
3. Returns `id`, `userName`, `email`, `firstName`, `lastName`, `phoneNumber`, and `roles` (from the current token claims).

### Test Cases

| Test Case ID | Description                                                   | Status  |
| ------------ | ------------------------------------------------------------- | ------- |
| GET-INFO-001 | Valid token returns 200 with profile data                     | ✅ Pass |
| GET-INFO-002 | Response contains correct userName and email when they differ | ✅ Pass |
| GET-INFO-003 | Missing token returns 401 Unauthorized                        | ✅ Pass |
| GET-INFO-004 | Token expired returns 401 Unauthorized                        | ✅ Pass |

---

## Update User Info

**Endpoint:** `PUT /api/auth/info`
**Auth:** Required

### Use Case

As an authenticated user, I want to update my profile information (first name, last name, phone number), so that I can keep my personal details current.

### Description

1. Client sends `firstName`, `lastName`, and/or `phoneNumber` with a valid access token.
2. The server loads the current user and updates the provided fields.
3. Returns the user's `id`.

### Test Cases

| Test Case ID    | Description                                                   | Status  |
| --------------- | ------------------------------------------------------------- | ------- |
| UPDATE-INFO-001 | Missing token returns 401 Unauthorized                        | ✅ Pass |
| UPDATE-INFO-002 | Valid data returns 200                                        | ✅ Pass |
| UPDATE-INFO-003 | Updated values are reflected in subsequent Get User Info call | ✅ Pass |
| UPDATE-INFO-004 | Phone number exceeding 20 characters returns 400 Bad Request  | ✅ Pass |
