import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import { authHeaders, createVerifiedUser, send, setupTestBindings } from "./support/test-helpers";
import type { AppBindings } from "../src/types";

describe("SourceBase.Workers habit log routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;

  beforeEach(async () => {
    app = createApp();
    bindings = await setupTestBindings();
  });

  it("requires auth for create and list", async () => {
    const createResponse = await send(app, bindings, "http://localhost/api/habit-logs", {
      method: "POST",
      body: JSON.stringify({ entries: [{ action: "HabitStarted", occurredAt: "2026-09-29T09:00:00.000Z" }] })
    });
    const getResponse = await send(app, bindings, "http://localhost/api/habit-logs");

    expect(createResponse.status).toBe(401);
    expect(getResponse.status).toBe(401);
  });

  it("creates logs and supports filtering, ignore, and paging", async () => {
    const user = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/habit-logs", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        entries: [
          { action: "HabitStarted", occurredAt: "2026-09-28T09:00:00.000Z" },
          { action: "HabitStarted", occurredAt: "2026-09-29T09:00:00.000Z" },
          { action: "Dismissed", occurredAt: "2026-09-29T10:00:00.000Z" },
          { action: "Snoozed", occurredAt: "2026-09-29T11:00:00.000Z" }
        ]
      })
    });
    expect(createResponse.status).toBe(200);
    const created = await createResponse.json() as { ids: string[] };
    expect(created.ids).toHaveLength(4);

    const filteredResponse = await send(
      app,
      bindings,
      "http://localhost/api/habit-logs?action=HabitStarted&from=2026-09-29T00:00:00.000Z&to=2026-09-29T23:59:59.999Z&page=1&limit=1&orderBy=OccurredAt&order=Asc",
      { headers: authHeaders(user.accessToken) }
    );
    expect(filteredResponse.status).toBe(200);
    const filtered = await filteredResponse.json() as {
      items: Array<{ action: string; occurredAt: string }>;
      page: number;
      limit: number;
      total: number;
    };
    expect(filtered.page).toBe(1);
    expect(filtered.limit).toBe(1);
    expect(filtered.total).toBe(1);
    expect(filtered.items).toEqual([expect.objectContaining({
      action: "HabitStarted",
      occurredAt: "2026-09-29T09:00:00.000Z"
    })]);

    const ignoreResponse = await send(
      app,
      bindings,
      "http://localhost/api/habit-logs?ignore=Dismissed&ignore=Snoozed&orderBy=OccurredAt&order=Asc",
      { headers: authHeaders(user.accessToken) }
    );
    expect(ignoreResponse.status).toBe(200);
    const ignored = await ignoreResponse.json() as { total: number; items: Array<{ action: string }> };
    expect(ignored.total).toBe(2);
    expect(ignored.items.every(item => item.action === "HabitStarted")).toBe(true);
  });

  it("scopes logs to the current user and deletes only the caller's entries", async () => {
    const owner = await createVerifiedUser(app, bindings);
    const other = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/habit-logs", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({ entries: [{ action: "Dismissed", occurredAt: "2026-09-29T11:00:00.000Z" }] })
    });
    const created = await createResponse.json() as { ids: string[] };

    const otherListResponse = await send(app, bindings, "http://localhost/api/habit-logs", {
      headers: authHeaders(other.accessToken)
    });
    const otherList = await otherListResponse.json() as { items: Array<{ id: string }> };
    expect(otherList.items.some(item => item.id === created.ids[0])).toBe(false);

    const otherDeleteResponse = await send(app, bindings, `http://localhost/api/habit-logs/${created.ids[0]}`, {
      method: "DELETE",
      headers: authHeaders(other.accessToken)
    });
    expect(otherDeleteResponse.status).toBe(404);

    const ownerDeleteResponse = await send(app, bindings, `http://localhost/api/habit-logs/${created.ids[0]}`, {
      method: "DELETE",
      headers: authHeaders(owner.accessToken)
    });
    expect(ownerDeleteResponse.status).toBe(200);
    expect(await ownerDeleteResponse.json()).toEqual({ success: true });
  });
});
