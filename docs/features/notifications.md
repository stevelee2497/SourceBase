# Notification Features

Real-time notification system that alerts users of important events. Notifications are stored in the database and pushed to connected clients via SignalR. All REST endpoints are under `/api/notifications` and require authentication. The SignalR hub is available at `/hubs/notifications`.

---

## Get Notifications

**Endpoint:** `GET /api/notifications`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve my notifications, so that I can stay informed about important events that occurred while I was away or before I connected via SignalR.

### Description

1. Client may provide optional query parameters: `page`, `limit`, and `unreadOnly` (boolean).
2. Returns a paginated list of the current user's notifications ordered by creation date descending.
3. Each notification includes `id`, `title`, `message`, `isRead`, and `createdOn`.
4. Notifications from other users are never included.

### Test Cases

| Test Case ID    | Description                                                   | Status      |
| --------------- | ------------------------------------------------------------- | ----------- |
| NOTIF-GET-001   | Missing token returns 401 Unauthorized                        | ✅ Pass     |
| NOTIF-GET-002   | Authenticated user receives their own notifications           | ✅ Pass     |
| NOTIF-GET-003   | `unreadOnly=true` returns only unread notifications           | ✅ Pass     |
| NOTIF-GET-004   | Returns empty list when user has no notifications             | ✅ Pass     |
| NOTIF-GET-005   | Another user's notifications are not included in the response | ✅ Pass     |

---

## Mark Notification As Read

**Endpoint:** `PUT /api/notifications/{id}/read`
**Auth:** Required

### Use Case

As an authenticated user, I want to mark a specific notification as read by clicking on it, so that I can track which notifications I have already seen.

### Description

1. Client provides the notification `id` as a route parameter.
2. If the notification does not exist or belongs to a different user → `404 Not Found`.
3. Sets `IsRead = true` on the notification record.
4. Returns `{ success: true }`.

### Test Cases

| Test Case ID         | Description                                                 | Status      |
| -------------------- | ----------------------------------------------------------- | ----------- |
| NOTIF-MARK-READ-001  | Missing token returns 401 Unauthorized                      | ✅ Pass     |
| NOTIF-MARK-READ-002  | Valid ID marks the notification as read and returns 200     | ✅ Pass     |
| NOTIF-MARK-READ-003  | Non-existent notification ID returns 404 Not Found          | ✅ Pass     |
| NOTIF-MARK-READ-004  | Another user's notification ID returns 404 Not Found        | ✅ Pass     |
| NOTIF-MARK-READ-005  | Already-read notification can be marked read again (no-op)  | ✅ Pass     |

---

## Mark All Notifications As Read

**Endpoint:** `PUT /api/notifications/read-all`
**Auth:** Required

### Use Case

As an authenticated user, I want to mark all my notifications as read at once, so that I can quickly clear the unread indicator without clicking each notification individually.

### Description

1. Finds all unread notifications belonging to the current user.
2. Sets `IsRead = true` on all of them in a single update.
3. Returns `{ success: true }`.

### Test Cases

| Test Case ID              | Description                                                 | Status      |
| ------------------------- | ----------------------------------------------------------- | ----------- |
| NOTIF-MARK-ALL-READ-001   | Missing token returns 401 Unauthorized                      | ✅ Pass     |
| NOTIF-MARK-ALL-READ-002   | Marks all unread notifications as read and returns 200      | ✅ Pass     |
| NOTIF-MARK-ALL-READ-003   | After the call, unreadOnly query returns an empty list      | ✅ Pass     |
| NOTIF-MARK-ALL-READ-004   | Only the current user's notifications are affected          | ✅ Pass     |

---

## Clear All Notifications

**Endpoint:** `DELETE /api/notifications`
**Auth:** Required

### Use Case

As an authenticated user, I want to clear all my notifications, so that I can keep my notification list clean and remove events I no longer care about.

### Description

1. Deletes all notifications belonging to the current user.
2. Returns `{ success: true }`.

### Test Cases

| Test Case ID      | Description                                                       | Status      |
| ----------------- | ----------------------------------------------------------------- | ----------- |
| NOTIF-CLEAR-001   | Missing token returns 401 Unauthorized                            | ✅ Pass     |
| NOTIF-CLEAR-002   | Clears all notifications for the current user and returns 200     | ✅ Pass     |
| NOTIF-CLEAR-003   | After the call, Get Notifications returns an empty list           | ✅ Pass     |
| NOTIF-CLEAR-004   | Only the current user's notifications are deleted                 | ✅ Pass     |

---

## SignalR Hub

**Hub URL:** `/hubs/notifications`
**Auth:** Required (JWT bearer token passed as `access_token` query parameter)

### Use Case

As an authenticated user with the web application open, I want to receive notifications in real-time without refreshing the page, so that I am immediately aware of new events.

### Description

1. Client connects to `/hubs/notifications` passing the JWT access token as the `access_token` query parameter.
2. On connection, the server places the user into a SignalR group identified by their `userId`.
3. When a new notification is created for a user, the server invokes `ReceiveNotification` on all connections in that user's group.
4. The client receives the notification payload: `{ id, title, message, createdOn }` and updates the bell icon and notification list in real-time.

### Client Event: `ReceiveNotification`

**Payload:**

| Field       | Type     | Description                          |
| ----------- | -------- | ------------------------------------ |
| `id`        | `Guid`   | The new notification's ID            |
| `title`     | `string` | Short title of the notification      |
| `message`   | `string` | Full notification message            |
| `createdOn` | `string` | ISO 8601 UTC timestamp of creation   |

### Notification Triggers

| Event                          | Recipients          | Title                      | Message                                         |
| ------------------------------ | ------------------- | -------------------------- | ----------------------------------------------- |
| User submits time sheets       | Submitting user     | Time Sheets Submitted      | Your time sheets have been submitted successfully. |
| Admin creates a new user       | All admin users     | New User Registered        | A new user `{email}` has been registered.        |
