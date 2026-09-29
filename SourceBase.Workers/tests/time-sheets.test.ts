import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import type { AppBindings } from "../src/types";
import { authHeaders, createVerifiedUser, send, setupTestBindings } from "./support/test-helpers";

describe("SourceBase.Workers time sheet routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;

  beforeEach(async () => {
    bindings = await setupTestBindings();
    app = createApp();
  });

  it("requires auth for writes", async () => {
    const response = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      body: JSON.stringify({
        items: [{ date: "2025-06-01", project: "ProjectA", hours: 8 }]
      })
    });
    expect(response.status).toBe(401);
  });

  it("upserts by user, date, and project and stores a notification row", async () => {
    const user = await createVerifiedUser(app, bindings);

    const firstResponse = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        items: [{ date: "2025-07-01", project: "UpsertProject", hours: 4 }]
      })
    });
    expect(firstResponse.status).toBe(200);
    const firstBody = await firstResponse.json() as { ids: string[] };

    const secondResponse = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        items: [{ date: "2025-07-01", project: "UpsertProject", hours: 8 }]
      })
    });
    expect(secondResponse.status).toBe(200);
    const secondBody = await secondResponse.json() as { ids: string[] };

    expect(secondBody.ids).toEqual(firstBody.ids);

    const getResponse = await send(app, bindings, `http://localhost/api/time-sheets/${firstBody.ids[0]}`, {
      headers: authHeaders(user.accessToken)
    });
    expect(getResponse.status).toBe(200);
    expect(await getResponse.json()).toMatchObject({
      id: firstBody.ids[0],
      project: "UpsertProject",
      hours: 8,
      date: "2025-07-01"
    });

    const userId = (await bindings.DB.prepare(`SELECT id FROM users WHERE lower(email) = lower(?)`).bind(user.email).first<string>("id"))!;
    const notifications = await bindings.DB.prepare(
      `SELECT title, message, event
       FROM notifications
       WHERE user_id = ?
       ORDER BY created_on DESC`
    )
      .bind(userId)
      .all<{ title: string; message: string; event: string }>();

    expect(notifications.results[0]).toMatchObject({
      title: "Time Sheets Submitted",
      message: "Your time sheets have been submitted successfully.",
      event: "GlobalNotificationEvent"
    });
  });

  it("filters and pages time sheets by month or exact date", async () => {
    const user = await createVerifiedUser(app, bindings);

    const seedResponse = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        items: [
          { date: "2025-06-10", project: "Alpha", hours: 4 },
          { date: "2025-06-15", project: "Zulu", hours: 8 },
          { date: "2025-07-01", project: "July", hours: 5 }
        ]
      })
    });
    expect(seedResponse.status).toBe(200);

    const monthResponse = await send(
      app,
      bindings,
      "http://localhost/api/time-sheets?year=2025&month=6&orderBy=Project&order=Desc&page=1&limit=1",
      { headers: authHeaders(user.accessToken) }
    );
    expect(monthResponse.status).toBe(200);
    const monthBody = await monthResponse.json() as {
      items: Array<{ project: string }>;
      total: number;
      page: number;
      limit: number;
    };

    expect(monthBody.total).toBe(2);
    expect(monthBody.page).toBe(1);
    expect(monthBody.limit).toBe(1);
    expect(monthBody.items[0]?.project).toBe("Zulu");

    const dateResponse = await send(app, bindings, "http://localhost/api/time-sheets?date=2025-06-10", {
      headers: authHeaders(user.accessToken)
    });
    expect(dateResponse.status).toBe(200);
    const dateBody = await dateResponse.json() as { items: Array<{ project: string }> };
    expect(dateBody.items.map(item => item.project)).toEqual(["Alpha"]);
  });

  it("validates time sheet payloads and deletes only the caller's entry", async () => {
    const owner = await createVerifiedUser(app, bindings);
    const stranger = await createVerifiedUser(app, bindings);

    const invalidResponse = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({
        items: [{ date: "2025-06-01", project: "Too Much", hours: 9 }]
      })
    });
    expect(invalidResponse.status).toBe(400);

    const createResponse = await send(app, bindings, "http://localhost/api/time-sheets", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({
        items: [{ date: "2025-12-01", project: "OwnerEntry", hours: 8 }]
      })
    });
    const created = await createResponse.json() as { ids: string[] };

    const strangerDelete = await send(app, bindings, `http://localhost/api/time-sheets/${created.ids[0]}`, {
      method: "DELETE",
      headers: authHeaders(stranger.accessToken)
    });
    expect(strangerDelete.status).toBe(404);

    const deleteResponse = await send(app, bindings, `http://localhost/api/time-sheets/${created.ids[0]}`, {
      method: "DELETE",
      headers: authHeaders(owner.accessToken)
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });
  });
});
