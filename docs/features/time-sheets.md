# Time Sheet Features

Endpoints for logging and reviewing time entries per project per day. All endpoints require authentication. Time sheet entries are scoped to the authenticated user — users cannot see or modify each other's data.

All Time Sheet endpoints are under `/api/time-sheets`.

---

## Bulk Upsert Time Sheets

**Endpoint:** `POST /api/time-sheets`
**Auth:** Required

### Use Case

As an authenticated user, I want to log one or more time entries for specific dates and projects in a single request, so that I can record or update my work hours efficiently.

### Description

1. Client sends a list of items, each with `date` (ISO 8601), `project` (required, non-empty string), and `hours` (decimal, > 0, ≤ 24).
2. For each item, if an entry for the same `(user, date, project)` combination already exists, its `hours` value is updated (upsert).
3. If no matching entry exists, a new record is created.
4. Returns a list of affected entry IDs (either newly created or updated).
5. After saving, a notification is created for the submitting user with title "Time Sheets Submitted".

### Test Cases

| Test Case ID         | Description                                            | Status  |
| -------------------- | ------------------------------------------------------ | ------- |
| TIMESHEET-CREATE-001 | Missing token returns 401 Unauthorized                 | ✅ Pass |
| TIMESHEET-CREATE-002 | Valid list of items creates entries and returns IDs    | ✅ Pass |
| TIMESHEET-CREATE-003 | Re-submitting same date+project updates hours (upsert) | ✅ Pass |
| TIMESHEET-CREATE-004 | Empty items list returns 400 Bad Request               | ✅ Pass |
| TIMESHEET-CREATE-005 | Missing project returns 400 Bad Request                | ✅ Pass |
| TIMESHEET-CREATE-006 | Hours = 0 returns 400 Bad Request                      | ✅ Pass |
| TIMESHEET-CREATE-007 | Hours > 8 returns 400 Bad Request                      | ✅ Pass |
| TIMESHEET-CREATE-008 | Multiple items in one request all upsert correctly     | ✅ Pass |
| TIMESHEET-CREATE-009 | Entry created by user A is not visible to user B       | ✅ Pass |
| TIMESHEET-CREATE-010 | Successful submission creates a notification for the submitting user | ✅ Pass |

---

## List Time Sheets

**Endpoint:** `GET /api/time-sheets`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve my time entries for a given date or month, so that I can review or manage my logged hours.

### Description

1. Client may filter by `date` (exact day), `year`, or `month` query parameters.
2. Returns only entries belonging to the authenticated user — other users' entries are never included.
3. Supports pagination via `page` and `limit` query parameters.

### Test Cases

| Test Case ID          | Description                                       | Status  |
| --------------------- | ------------------------------------------------- | ------- |
| TIMESHEET-GET-ALL-001 | Missing token returns 401 Unauthorized            | ✅ Pass |
| TIMESHEET-GET-ALL-002 | Returns entries filtered by year and month        | ✅ Pass |
| TIMESHEET-GET-ALL-003 | Returns entries filtered by specific date         | ✅ Pass |
| TIMESHEET-GET-ALL-004 | Only the current user's entries are returned      | ✅ Pass |
| TIMESHEET-GET-ALL-005 | Empty result when no entries exist for the filter | ✅ Pass |

---

## Get Time Sheet

**Endpoint:** `GET /api/time-sheets/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the details of a specific time entry by its ID, so that I can inspect or edit it.

### Description

1. Client provides the entry `id` as a route parameter.
2. If the entry does not exist or belongs to a different user → `404 Not Found`.
3. Returns the full entry: `id`, `date`, `project`, `hours`, and audit fields.

### Test Cases

| Test Case ID      | Description                                   | Status  |
| ----------------- | --------------------------------------------- | ------- |
| TIMESHEET-GET-001 | Missing token returns 401 Unauthorized        | ✅ Pass |
| TIMESHEET-GET-002 | Valid ID returns the entry with correct data  | ✅ Pass |
| TIMESHEET-GET-003 | Non-existent ID returns 404 Not Found         | ✅ Pass |
| TIMESHEET-GET-004 | Another user's entry ID returns 404 Not Found | ✅ Pass |

---

## Delete Time Sheet

**Endpoint:** `DELETE /api/time-sheets/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete a time entry I no longer need, so that my records remain accurate.

### Description

1. Client provides the entry `id` as a route parameter.
2. If the entry does not exist or belongs to a different user → `404 Not Found`.
3. The entry is permanently removed from the database.

### Test Cases

| Test Case ID         | Description                                   | Status  |
| -------------------- | --------------------------------------------- | ------- |
| TIMESHEET-DELETE-001 | Missing token returns 401 Unauthorized        | ✅ Pass |
| TIMESHEET-DELETE-002 | Valid ID deletes the entry and returns 200    | ✅ Pass |
| TIMESHEET-DELETE-003 | Non-existent ID returns 404 Not Found         | ✅ Pass |
| TIMESHEET-DELETE-004 | Another user's entry ID returns 404 Not Found | ✅ Pass |

---

## Get Time Sheet Summary

**Endpoint:** `GET /api/time-sheets/summary`
**Auth:** Required

### Use Case

As an authenticated user, I want a lightweight per-day aggregate of my time entries for a given month, so that the calendar view can efficiently render which days have data without loading every individual record.

### Description

1. Client provides required `year` and `month` query parameters (year 2000–2100, month 1–12).
2. Returns a list of day summaries, one per day that has at least one entry.
3. Each summary contains: `date`, `totalHours` (sum of all entry hours for that day), and `projects` (sorted list of distinct project names logged that day).
4. Days with no entries are omitted from the response.

### Test Cases

| Test Case ID          | Description                                             | Status  |
| --------------------- | ------------------------------------------------------- | ------- |
| TIMESHEET-SUMMARY-001 | Missing token returns 401 Unauthorized                  | ✅ Pass |
| TIMESHEET-SUMMARY-002 | Returns correct day aggregates for a month with entries | ✅ Pass |
| TIMESHEET-SUMMARY-003 | Empty month returns empty days list                     | ✅ Pass |
| TIMESHEET-SUMMARY-004 | Only includes entries for the authenticated user        | ✅ Pass |
| TIMESHEET-SUMMARY-005 | Missing required year/month returns 400 Bad Request     | ✅ Pass |
