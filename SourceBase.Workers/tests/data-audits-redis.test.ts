import { beforeEach, describe, expect, it } from "vitest";
import { createTestContext, getAdminAccessToken, send } from "./helpers";
import type { TestContext } from "./helpers";

describe("SourceBase.Workers data audits and redis routes", () => {
  let context: TestContext;

  beforeEach(async () => {
    context = await createTestContext();
  });

  it("requires admin for audits and returns paged audit rows", async () => {
    const adminToken = await getAdminAccessToken(context);
    const memberToken = await createMemberToken(context);

    await context.bindings.DB.prepare(
      `INSERT INTO audit_histories (id, author, action, action_on, entity_type, entity_id, current, original, changes)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        crypto.randomUUID(),
        "admin-qt@yopmail.com",
        "Modified",
        "2026-09-29T08:00:00.000Z",
        "UserEntity",
        crypto.randomUUID(),
        JSON.stringify({ email: "after@test.com" }),
        JSON.stringify({ email: "before@test.com" }),
        JSON.stringify([{ property: "Email", current: "after@test.com", original: "before@test.com" }]),
        crypto.randomUUID(),
        "admin-qt@yopmail.com",
        "Added",
        "2026-09-29T07:00:00.000Z",
        "RoleEntity",
        crypto.randomUUID(),
        JSON.stringify({ name: "Manager" }),
        null,
        JSON.stringify([{ property: "Name", current: "Manager", original: null }])
      )
      .run();

    const anonymousResponse = await send(context, "http://localhost/api/data/audits");
    expect(anonymousResponse.status).toBe(401);

    const memberResponse = await send(context, "http://localhost/api/data/audits", {
      headers: { Authorization: `Bearer ${memberToken}` }
    });
    expect(memberResponse.status).toBe(403);

    const adminResponse = await send(context, "http://localhost/api/data/audits?page=1&limit=1&order=Desc", {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    expect(adminResponse.status).toBe(200);
    const adminBody = await adminResponse.json() as {
      items: Array<{
        action: string;
        entityType: string;
        current: Record<string, string> | null;
        original: Record<string, string> | null;
        changes: Array<{ property: string; current: string; original: string | null }> | null;
      }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(adminBody.page).toBe(1);
    expect(adminBody.limit).toBe(1);
    expect(adminBody.total).toBe(2);
    expect(adminBody.items[0]).toMatchObject({
      action: "Modified",
      entityType: "UserEntity",
      current: { email: "after@test.com" },
      original: { email: "before@test.com" }
    });
    expect(adminBody.items[0].changes?.[0]).toMatchObject({
      property: "Email",
      current: "after@test.com",
      original: "before@test.com"
    });
  });

  it("requires auth for redis status and reflects the configured flag", async () => {
    const adminToken = await getAdminAccessToken(context);

    const anonymousResponse = await send(context, "http://localhost/api/data/redis-status");
    expect(anonymousResponse.status).toBe(401);

    const offlineResponse = await send(context, "http://localhost/api/data/redis-status", {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    expect(offlineResponse.status).toBe(200);
    expect(await offlineResponse.json()).toEqual({ isOnline: false });

    context.bindings.REDIS_AVAILABLE = "true";
    const onlineResponse = await send(context, "http://localhost/api/data/redis-status", {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    expect(onlineResponse.status).toBe(200);
    expect(await onlineResponse.json()).toEqual({ isOnline: true });
  });
});

async function createMemberToken(context: TestContext) {
  const email = `member_${crypto.randomUUID()}@test.com`;
  const password = "Member@1234";

  await send(context, "http://localhost/api/auth/register", {
    method: "POST",
    body: JSON.stringify({
      userName: `member_${crypto.randomUUID().toString().replaceAll("-", "")}`,
      email,
      password
    })
  });

  const otp = await context.bindings.DB.prepare(`SELECT otp_code FROM users WHERE lower(email) = lower(?)`)
    .bind(email)
    .first<{ otp_code: string }>();

  await send(context, "http://localhost/api/auth/confirmEmail", {
    method: "POST",
    body: JSON.stringify({ email, code: otp?.otp_code })
  });

  const loginResponse = await send(context, "http://localhost/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
  const loginBody = await loginResponse.json() as { accessToken: string };
  return loginBody.accessToken;
}
