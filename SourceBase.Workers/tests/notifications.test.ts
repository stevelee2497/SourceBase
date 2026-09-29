import { beforeEach, describe, expect, it } from "vitest";
import { authHeaders, createTestContext, createUserAndGetAccessToken, getAdminAccessToken, getUserByEmail, type TestContext } from "./helpers";

describe("SourceBase.Workers notifications routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("requires auth for notifications reads", async () => {
    const response = await context.app.fetch(new Request("http://localhost/api/notifications"), context.bindings, {
      props: {},
      waitUntil() {},
      passThroughOnException() {}
    });
    expect(response.status).toBe(401);
  });

  it("returns only current-user notifications with unread paging", async () => {
    const admin = await getUserByEmail(context, String(context.bindings.ADMIN_EMAIL));
    const otherToken = await createUserAndGetAccessToken(context);
    expect(otherToken).toBeTruthy();
    const otherUser = await context.bindings.DB.prepare(
      `SELECT id
       FROM users
       WHERE id != ?
       ORDER BY created_on DESC
       LIMIT 1`
    )
      .bind(admin?.id)
      .first<{ id: string }>();

    await insertNotification(context, admin!.id, { title: "Read", isRead: 1, createdOn: "2026-01-01T00:00:00.000Z" });
    await insertNotification(context, admin!.id, { title: "Unread 1", isRead: 0, createdOn: "2026-01-02T00:00:00.000Z" });
    await insertNotification(context, admin!.id, { title: "Unread 2", isRead: 0, createdOn: "2026-01-03T00:00:00.000Z" });
    await insertNotification(context, otherUser!.id, { title: "Other User", isRead: 0, createdOn: "2026-01-04T00:00:00.000Z" });

    const response = await fetchWithContext(context, "http://localhost/api/notifications?unreadOnly=true&page=1&limit=2", {
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ title: string; isRead: boolean }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(body.page).toBe(1);
    expect(body.limit).toBe(2);
    expect(body.total).toBe(2);
    expect(body.items.map(item => item.title)).toEqual(["Unread 2", "Unread 1"]);
    expect(body.items.every(item => item.isRead === false)).toBe(true);
  });

  it("marks one notification as read", async () => {
    const admin = await getUserByEmail(context, String(context.bindings.ADMIN_EMAIL));
    const notificationId = await insertNotification(context, admin!.id, { title: "Mark Me", isRead: 0 });

    const response = await fetchWithContext(context, `http://localhost/api/notifications/${notificationId}/read`, {
      method: "PUT",
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({ success: true });
    const row = await context.bindings.DB.prepare(`SELECT is_read FROM notifications WHERE id = ?`)
      .bind(notificationId)
      .first<{ is_read: number }>();
    expect(row?.is_read).toBe(1);
  });

  it("marks all current-user notifications as read without touching others", async () => {
    const admin = await getUserByEmail(context, String(context.bindings.ADMIN_EMAIL));
    await createUserAndGetAccessToken(context);
    const otherUser = await context.bindings.DB.prepare(
      `SELECT id
       FROM users
       WHERE id != ?
       ORDER BY created_on DESC
       LIMIT 1`
    )
      .bind(admin?.id)
      .first<{ id: string }>();

    await insertNotification(context, admin!.id, { title: "Admin 1", isRead: 0 });
    await insertNotification(context, admin!.id, { title: "Admin 2", isRead: 0 });
    await insertNotification(context, otherUser!.id, { title: "Other", isRead: 0 });

    const response = await fetchWithContext(context, "http://localhost/api/notifications/read-all", {
      method: "PUT",
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    const adminUnread = await context.bindings.DB.prepare(`SELECT COUNT(*) AS value FROM notifications WHERE user_id = ? AND is_read = 0`)
      .bind(admin!.id)
      .first<number>("value");
    const otherUnread = await context.bindings.DB.prepare(`SELECT COUNT(*) AS value FROM notifications WHERE user_id = ? AND is_read = 0`)
      .bind(otherUser!.id)
      .first<number>("value");
    expect(Number(adminUnread ?? 0)).toBe(0);
    expect(Number(otherUnread ?? 0)).toBe(1);
  });

  it("clears all current-user notifications", async () => {
    const admin = await getUserByEmail(context, String(context.bindings.ADMIN_EMAIL));
    await insertNotification(context, admin!.id, { title: "Delete 1", isRead: 0 });
    await insertNotification(context, admin!.id, { title: "Delete 2", isRead: 1 });

    const response = await fetchWithContext(context, "http://localhost/api/notifications", {
      method: "DELETE",
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({ success: true });
    const remaining = await context.bindings.DB.prepare(`SELECT COUNT(*) AS value FROM notifications WHERE user_id = ?`)
      .bind(admin!.id)
      .first<number>("value");
    expect(Number(remaining ?? 0)).toBe(0);
  });
});

async function insertNotification(context: TestContext, userId: string, values: { title: string; isRead: number; createdOn?: string }) {
  const id = crypto.randomUUID();
  await context.bindings.DB.prepare(
    `INSERT INTO notifications (id, user_id, event, title, message, data, is_read, created_on, created_by)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`
  )
    .bind(id, userId, "GlobalNotificationEvent", values.title, `${values.title} message`, "", values.isRead, values.createdOn ?? new Date().toISOString(), "system")
    .run();
  return id;
}

function fetchWithContext(context: TestContext, url: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  return context.app.fetch(new Request(url, { ...init, headers }), context.bindings, {
    props: {},
    waitUntil() {},
    passThroughOnException() {}
  });
}
