import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import { authHeaders, createVerifiedUser, send, setupTestBindings } from "./support/test-helpers";
import type { AppBindings } from "../src/types";

describe("SourceBase.Workers habits routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;

  beforeEach(async () => {
    app = createApp();
    bindings = await setupTestBindings();
  });

  it("requires auth for list retrieval", async () => {
    const response = await send(app, bindings, "http://localhost/api/habits");
    expect(response.status).toBe(401);
  });

  it("creates habits, returns system habits, and counts logs inside the requested range", async () => {
    const user = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/habits", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Morning Run", icon: "🏃" })
    });
    expect(createResponse.status).toBe(200);
    const created = await createResponse.json() as { id: string };

    const logResponse = await send(app, bindings, "http://localhost/api/habit-logs", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        entries: [
          { habitId: created.id, habitName: "Morning Run", action: "HabitStarted", occurredAt: "2026-09-29T08:00:00.000Z" },
          { habitId: created.id, habitName: "Morning Run", action: "HabitStarted", occurredAt: "2026-09-20T08:00:00.000Z" }
        ]
      })
    });
    expect(logResponse.status).toBe(200);

    const response = await send(
      app,
      bindings,
      "http://localhost/api/habits?from=2026-09-29T00:00:00.000Z&to=2026-09-29T23:59:59.999Z",
      { headers: authHeaders(user.accessToken) }
    );
    expect(response.status).toBe(200);

    const habits = await response.json() as Array<{ id: string; name: string; icon: string | null; isSystem: boolean; logCount: number }>;
    expect(habits.some(habit => habit.isSystem && habit.name === "Walk")).toBe(true);
    expect(habits).toContainEqual(expect.objectContaining({
      id: created.id,
      name: "Morning Run",
      icon: "🏃",
      isSystem: false,
      logCount: 1
    }));
  });

  it("updates only owner habits and keeps null icon values unchanged", async () => {
    const owner = await createVerifiedUser(app, bindings);
    const other = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/habits", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({ name: "Hydrate", icon: "💧" })
    });
    const created = await createResponse.json() as { id: string };

    const ownerUpdateResponse = await send(app, bindings, `http://localhost/api/habits/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({ name: "Hydrate Daily", icon: null })
    });
    expect(ownerUpdateResponse.status).toBe(200);

    const otherUpdateResponse = await send(app, bindings, `http://localhost/api/habits/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(other.accessToken),
      body: JSON.stringify({ name: "Hacked" })
    });
    expect(otherUpdateResponse.status).toBe(404);

    const listResponse = await send(app, bindings, "http://localhost/api/habits", {
      headers: authHeaders(owner.accessToken)
    });
    const habits = await listResponse.json() as Array<{ id: string; name: string; icon: string | null }>;
    expect(habits).toContainEqual(expect.objectContaining({
      id: created.id,
      name: "Hydrate Daily",
      icon: "💧"
    }));
  });

  it("rejects writes to system habits and deletes owned custom habits", async () => {
    const user = await createVerifiedUser(app, bindings);

    const listResponse = await send(app, bindings, "http://localhost/api/habits", {
      headers: authHeaders(user.accessToken)
    });
    const habits = await listResponse.json() as Array<{ id: string; isSystem: boolean }>;
    const systemHabit = habits.find(habit => habit.isSystem);
    expect(systemHabit).toBeTruthy();

    const systemUpdateResponse = await send(app, bindings, `http://localhost/api/habits/${systemHabit?.id}`, {
      method: "PATCH",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Blocked" })
    });
    expect(systemUpdateResponse.status).toBe(403);

    const systemDeleteResponse = await send(app, bindings, `http://localhost/api/habits/${systemHabit?.id}`, {
      method: "DELETE",
      headers: authHeaders(user.accessToken)
    });
    expect(systemDeleteResponse.status).toBe(403);

    const createResponse = await send(app, bindings, "http://localhost/api/habits", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Stretch", icon: "🧘" })
    });
    const created = await createResponse.json() as { id: string };

    const deleteResponse = await send(app, bindings, `http://localhost/api/habits/${created.id}`, {
      method: "DELETE",
      headers: authHeaders(user.accessToken)
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });
  });
});
