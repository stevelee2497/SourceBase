import { beforeEach, describe, expect, it } from "vitest";
import { authHeaders, createTestContext, createUserAndGetAccessToken, getAdminAccessToken, getUserByEmail, send, type TestContext } from "./helpers";

describe("SourceBase.Workers machines routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("requires auth for machine creation", async () => {
    const response = await send(context, "http://localhost/api/machines", {
      method: "POST",
      body: JSON.stringify({ name: "unauthorized-machine" })
    });
    expect(response.status).toBe(401);
  });

  it("creates a machine and upserts its status on repeated create", async () => {
    const firstResponse = await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Desktop-01" })
    });
    expect(firstResponse.status).toBe(200);
    const firstBody = await firstResponse.json() as { id: string };

    await new Promise(resolve => setTimeout(resolve, 5));

    const secondResponse = await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Desktop-01", status: "Inactive" })
    });
    expect(secondResponse.status).toBe(200);
    const secondBody = await secondResponse.json() as { id: string };
    expect(secondBody.id).toBe(firstBody.id);

    const row = await context.bindings.DB.prepare(
      `SELECT status, last_reported_on
       FROM machines
       WHERE id = ?`
    )
      .bind(firstBody.id)
      .first<{ status: string; last_reported_on: string | null }>();

    expect(row?.status).toBe("Inactive");
    expect(row?.last_reported_on).toBeTruthy();
  });

  it("lists only current-user machines", async () => {
    await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Admin Machine", status: "Active" })
    });

    const otherToken = await createUserAndGetAccessToken(context);
    await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(otherToken),
      body: JSON.stringify({ name: "Other Machine", status: "Maintenance" })
    });

    const response = await send(context, "http://localhost/api/machines?page=1&limit=10", {
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ id: string; name: string; alias: string | null; status: string; lastReportedOn: string | null }>;
      total: number;
    };

    expect(body.total).toBe(1);
    expect(body.items).toHaveLength(1);
    expect(body.items[0]).toMatchObject({ name: "Admin Machine", alias: null, status: "Active" });
    expect(body.items[0].lastReportedOn).toBeTruthy();
  });

  it("updates alias while preserving status when null status is sent", async () => {
    const createResponse = await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Laptop", status: "Active" })
    });
    const created = await createResponse.json() as { id: string };

    const response = await send(context, `http://localhost/api/machines/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(token),
      body: JSON.stringify({ alias: "Work Laptop", status: null })
    });

    expect(response.status).toBe(200);
    const row = await context.bindings.DB.prepare(`SELECT alias, status FROM machines WHERE id = ?`)
      .bind(created.id)
      .first<{ alias: string | null; status: string }>();
    expect(row).toEqual({ alias: "Work Laptop", status: "Active" });
  });

  it("persists restart and shutdown command rows", async () => {
    const createResponse = await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Command Machine" })
    });
    const created = await createResponse.json() as { id: string };

    const restartResponse = await send(context, `http://localhost/api/machines/${created.id}/restart`, {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({})
    });
    const shutdownResponse = await send(context, `http://localhost/api/machines/${created.id}/shutdown`, {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({})
    });

    expect(await restartResponse.json()).toEqual({ message: "Restart command sent" });
    expect(await shutdownResponse.json()).toEqual({ message: "Shutdown command sent" });

    const rows = await context.bindings.DB.prepare(
      `SELECT type
       FROM machine_commands
       WHERE machine_id = ?
       ORDER BY created_on ASC, id ASC`
    )
      .bind(created.id)
      .all<{ type: string }>();

    expect(rows.results.map(row => row.type)).toEqual(["Restart", "Shutdown"]);
  });

  it("deletes only the current user's machine", async () => {
    const admin = await getUserByEmail(context, String(context.bindings.ADMIN_EMAIL));
    await createUserAndGetAccessToken(context);
    const otherUser = await context.bindings.DB.prepare(
      `SELECT id, user_name
       FROM users
       WHERE id != ?
       ORDER BY created_on DESC
       LIMIT 1`
    )
      .bind(admin?.id)
      .first<{ id: string; user_name: string }>();

    const ownMachineResponse = await send(context, "http://localhost/api/machines", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ name: "Delete Mine" })
    });
    const ownMachine = await ownMachineResponse.json() as { id: string };

    await context.bindings.DB.prepare(
      `INSERT INTO machines (id, name, alias, status, last_reported_on, user_id, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(crypto.randomUUID(), "Other User Machine", null, "Active", new Date().toISOString(), otherUser!.id, new Date().toISOString(), otherUser!.user_name)
      .run();

    const deleteResponse = await send(context, `http://localhost/api/machines/${ownMachine.id}`, {
      method: "DELETE",
      headers: authHeaders(token)
    });

    expect(deleteResponse.status).toBe(200);
    const ownRemaining = await context.bindings.DB.prepare(`SELECT COUNT(*) AS value FROM machines WHERE user_id = ?`)
      .bind(admin!.id)
      .first<number>("value");
    const otherRemaining = await context.bindings.DB.prepare(`SELECT COUNT(*) AS value FROM machines WHERE user_id = ?`)
      .bind(otherUser!.id)
      .first<number>("value");
    expect(Number(ownRemaining ?? 0)).toBe(0);
    expect(Number(otherRemaining ?? 0)).toBe(1);
  });
});
