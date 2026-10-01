import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import { bootstrapBaseSchema, getUserRoles, nowIso, seedBaseData } from "../src/lib/db";
import { hashPassword } from "../src/lib/security";
import { FakeD1Database } from "../src/testing/fake-d1";
import type { AppBindings } from "../src/types";

const adminEmail = "admin-qt@yopmail.com";
const adminPassword = "pw_test_admin";

describe("SourceBase.Workers users, roles, and data routes", () => {
  let bindings: AppBindings;

  beforeEach(async () => {
    bindings = {
      DB: new FakeD1Database(),
      JWT_SECRET: "jwt-secret-for-tests",
      REFRESH_TOKEN_SECRET: "refresh-secret-for-tests",
      ADMIN_EMAIL: adminEmail,
      ADMIN_PASSWORD: adminPassword,
      ACCESS_TOKEN_EXPIRES_IN: "3600",
      REFRESH_TOKEN_EXPIRES_IN: "2592000",
      OTP_TOKEN_EXPIRATION_MINUTES: "15"
    };

    await bootstrapBaseSchema(bindings.DB);
    await seedBaseData(bindings.DB, bindings);
  });

  it("roles and enums endpoints are public", async () => {
    const app = createApp();

    const rolesResponse = await send(app, bindings, "http://localhost/api/roles");
    expect(rolesResponse.status).toBe(200);
    const rolesBody = await rolesResponse.json() as {
      items: Array<{ name: string; description: string }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(rolesBody.page).toBe(1);
    expect(rolesBody.limit).toBe(10);
    expect(rolesBody.total).toBeGreaterThanOrEqual(2);
    expect(rolesBody.items.map(item => item.name)).toEqual(expect.arrayContaining(["Admin", "User"]));

    const enumsResponse = await send(app, bindings, "http://localhost/api/data/enums", {
      method: "POST",
      body: JSON.stringify({ enums: ["RolesOrder", "TodoItemStatus", "Roles"] })
    });

    expect(enumsResponse.status).toBe(200);
    const enumsBody = await enumsResponse.json() as {
      data: Record<string, Array<{ name: string; description: string | null }>>;
    };

    expect(enumsBody.data.RolesOrder.map(item => item.name)).toContain("CreatedOn");
    expect(enumsBody.data.TodoItemStatus.map(item => item.name)).toEqual(["Open", "Completed", "Archived"]);
    expect(enumsBody.data.Roles.map(item => item.name)).toEqual(expect.arrayContaining(["Admin", "User"]));
  });

  it("users and protected role endpoints reject anonymous and non-admin callers", async () => {
    const app = createApp();

    const anonymousUsersResponse = await send(app, bindings, "http://localhost/api/users");
    expect(anonymousUsersResponse.status).toBe(401);

    const userId = crypto.randomUUID();
    const userRoleId = await bindings.DB.prepare(`SELECT id FROM roles WHERE name = ?`).bind("User").first<string>("id");

    await bindings.DB.prepare(
      `INSERT INTO users (
         id, user_name, email, email_confirmed, password_hash, security_stamp, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        userId,
        "member-user",
        "member-user@test.com",
        1,
        await hashPassword("Member@1234"),
        crypto.randomUUID(),
        nowIso(),
        "system"
      )
      .run();

    await bindings.DB.prepare(`INSERT INTO user_roles (user_id, role_id) VALUES (?, ?)`).bind(userId, userRoleId).run();

    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "member-user@test.com", password: "Member@1234" })
    });
    const loginBody = await loginResponse.json() as { accessToken: string };

    const usersResponse = await send(app, bindings, "http://localhost/api/users", {
      headers: { Authorization: `Bearer ${loginBody.accessToken}` }
    });
    expect(usersResponse.status).toBe(403);

    const createRoleResponse = await send(app, bindings, "http://localhost/api/roles", {
      method: "POST",
      headers: { Authorization: `Bearer ${loginBody.accessToken}` },
      body: JSON.stringify({ name: "Manager", description: "Manager role" })
    });
    expect(createRoleResponse.status).toBe(403);
  });

  it("admin can create update and delete roles while admin role stays protected", async () => {
    const app = createApp();
    const token = await getAdminAccessToken(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/roles", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ name: "Manager", description: "Manager role" })
    });
    expect(createResponse.status).toBe(200);
    const createBody = await createResponse.json() as { id: string };

    const updateResponse = await send(app, bindings, `http://localhost/api/roles/${createBody.id}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ description: "Updated manager role" })
    });
    expect(updateResponse.status).toBe(200);

    const getRolesResponse = await send(app, bindings, "http://localhost/api/roles?order=Asc&orderBy=Name");
    const getRolesBody = await getRolesResponse.json() as { items: Array<{ id: string; name: string; description: string }> };
    expect(getRolesBody.items.find(item => item.id === createBody.id)).toEqual({
      id: createBody.id,
      name: "Manager",
      description: "Updated manager role"
    });

    const adminRoleId = await bindings.DB.prepare(`SELECT id FROM roles WHERE name = ?`).bind("Admin").first<string>("id");

    const updateAdminResponse = await send(app, bindings, `http://localhost/api/roles/${adminRoleId}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ description: "Blocked" })
    });
    expect(updateAdminResponse.status).toBe(400);
    expect((await updateAdminResponse.json() as { message: string }).message).toBe("Admin role cannot be updated.");

    const deleteResponse = await send(app, bindings, `http://localhost/api/roles/${createBody.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.status).toBe(200);
    expect((await deleteResponse.json() as { success: boolean }).success).toBe(true);

    const deleteAdminResponse = await send(app, bindings, `http://localhost/api/roles/${adminRoleId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteAdminResponse.status).toBe(400);
    expect((await deleteAdminResponse.json() as { message: string }).message).toBe("Role not found or cannot delete Admin role.");
  });

  it("admin can manage users, role assignments, email confirmation, password reset, and deletion", async () => {
    const app = createApp();
    const token = await getAdminAccessToken(app, bindings);

    const roleResponse = await send(app, bindings, "http://localhost/api/roles", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ name: "Manager", description: "Manager role" })
    });
    const roleBody = await roleResponse.json() as { id: string };

    const createResponse = await send(app, bindings, "http://localhost/api/users", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        userName: "created-user",
        email: "created-user@test.com",
        password: "Created@1234",
        firstName: "Created",
        lastName: "User",
        phoneNumber: "0123456789",
        roles: [" Manager ", "manager"]
      })
    });
    expect(createResponse.status).toBe(200);
    const createBody = await createResponse.json() as { id: string };

    const createdUserRoles = await getUserRoles(bindings.DB, createBody.id);
    expect(createdUserRoles).toEqual(["Manager"]);

    const confirmationEmail = await bindings.DB.prepare(
      `SELECT subject, body FROM emails WHERE "to" = ? ORDER BY sent_on DESC LIMIT 1`
    )
      .bind("created-user@test.com")
      .first<{ subject: string; body: string }>();
    expect(confirmationEmail?.subject).toBe("Confirm your email");
    expect(confirmationEmail?.body).toContain("confirmation code");

    const notificationCount = await bindings.DB.prepare(`SELECT COUNT(*) AS count FROM notifications WHERE title = ?`)
      .bind("New User Registered")
      .first<number>("count");
    expect(notificationCount).toBeGreaterThan(0);

    const usersResponse = await send(app, bindings, "http://localhost/api/users?order=Asc&orderBy=UserName", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(usersResponse.status).toBe(200);
    const usersBody = await usersResponse.json() as {
      items: Array<{ id: string; userName: string; emailConfirmed: boolean; roles: string[] }>;
    };
    expect(usersBody.items.find(item => item.id === createBody.id)).toMatchObject({
      id: createBody.id,
      userName: "created-user",
      emailConfirmed: false,
      roles: ["Manager"]
    });

    const previousSecurityStamp = await bindings.DB.prepare(`SELECT security_stamp FROM users WHERE id = ?`)
      .bind(createBody.id)
      .first<string>("security_stamp");

    const updateResponse = await send(app, bindings, `http://localhost/api/users/${createBody.id}`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        email: "created-user-updated@test.com",
        firstName: null,
        lastName: "Updated",
        phoneNumber: null,
        avatarUrl: "https://example.com/avatar.png",
        roles: ["User"]
      })
    });
    expect(updateResponse.status).toBe(200);

    const updatedUser = await bindings.DB.prepare(
      `SELECT email, email_confirmed, first_name, last_name, phone_number, avatar_url, otp_code, security_stamp
       FROM users
       WHERE id = ?`
    )
      .bind(createBody.id)
      .first<{
        email: string;
        email_confirmed: number;
        first_name: string | null;
        last_name: string | null;
        phone_number: string | null;
        avatar_url: string | null;
        otp_code: string | null;
        security_stamp: string;
      }>();

    expect(updatedUser).toMatchObject({
      email: "created-user-updated@test.com",
      email_confirmed: 0,
      first_name: null,
      last_name: "Updated",
      phone_number: null,
      avatar_url: "https://example.com/avatar.png",
      otp_code: expect.any(String)
    });
    expect(updatedUser?.security_stamp).not.toBe(previousSecurityStamp);

    const updatedRoles = await getUserRoles(bindings.DB, createBody.id);
    expect(updatedRoles).toEqual(["User"]);

    const confirmResponse = await send(app, bindings, `http://localhost/api/users/${createBody.id}/confirm-email`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(confirmResponse.status).toBe(200);
    expect((await confirmResponse.json() as { success: boolean }).success).toBe(true);

    const afterConfirm = await bindings.DB.prepare(`SELECT email_confirmed, otp_code, otp_code_expires_on FROM users WHERE id = ?`)
      .bind(createBody.id)
      .first<{ email_confirmed: number; otp_code: string | null; otp_code_expires_on: string | null }>();
    expect(afterConfirm).toEqual({ email_confirmed: 1, otp_code: null, otp_code_expires_on: null });

    const resetResponse = await send(app, bindings, `http://localhost/api/users/${createBody.id}/reset-password`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ newPassword: "Reset@1234" })
    });
    expect(resetResponse.status).toBe(200);
    expect((await resetResponse.json() as { success: boolean }).success).toBe(true);

    const resetEmail = await bindings.DB.prepare(
      `SELECT subject, body FROM emails WHERE "to" = ? ORDER BY sent_on DESC LIMIT 1`
    )
      .bind("created-user-updated@test.com")
      .first<{ subject: string; body: string }>();
    expect(resetEmail?.subject).toBe("Your password has been reset");
    expect(resetEmail?.body).toContain("Reset@1234");

    const loginOldPasswordResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "created-user-updated@test.com", password: "Created@1234" })
    });
    expect(loginOldPasswordResponse.status).toBe(401);

    const loginNewPasswordResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "created-user-updated@test.com", password: "Reset@1234" })
    });
    expect(loginNewPasswordResponse.status).toBe(200);

    const deleteResponse = await send(app, bindings, `http://localhost/api/users/${createBody.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.status).toBe(200);
    expect((await deleteResponse.json() as { success: boolean }).success).toBe(true);

    const deletedUser = await bindings.DB.prepare(`SELECT id FROM users WHERE id = ?`).bind(createBody.id).first<string>("id");
    expect(deletedUser).toBeNull();

    const duplicateResponse = await send(app, bindings, "http://localhost/api/users", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        userName: "duplicate-user",
        email: adminEmail,
        password: "Created@1234"
      })
    });
    expect(duplicateResponse.status).toBe(400);
    expect((await duplicateResponse.json() as { message: string }).message).toBe("Email is already taken.");

    const roleExists = await bindings.DB.prepare(`SELECT id FROM roles WHERE id = ?`).bind(roleBody.id).first<string>("id");
    expect(roleExists).not.toBeNull();
  });

  it("stats endpoint requires authentication and returns current-user aggregates", async () => {
    const app = createApp();
    const anonymousStatsResponse = await send(app, bindings, "http://localhost/api/data/stats");
    expect(anonymousStatsResponse.status).toBe(401);

    const userRoleId = await bindings.DB.prepare(`SELECT id FROM roles WHERE name = ?`).bind("User").first<string>("id");
    const statsUserId = crypto.randomUUID();
    const otherUserId = crypto.randomUUID();

    await bindings.DB.prepare(
      `INSERT INTO users (
         id, user_name, email, email_confirmed, password_hash, security_stamp, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        statsUserId,
        "stats-user",
        "stats-user@test.com",
        1,
        await hashPassword("Stats@1234"),
        crypto.randomUUID(),
        nowIso(),
        "system",
        otherUserId,
        "other-user",
        "other-user@test.com",
        1,
        await hashPassword("Other@1234"),
        crypto.randomUUID(),
        nowIso(),
        "system"
      )
      .run();

    await bindings.DB.prepare(`INSERT INTO user_roles (user_id, role_id) VALUES (?, ?), (?, ?)`)
      .bind(statsUserId, userRoleId, otherUserId, userRoleId)
      .run();

    const today = getCurrentUtcDate();
    const monthStart = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
    const nextMonthStart = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() + 1, 1));

    const walletId = crypto.randomUUID();
    const otherWalletId = crypto.randomUUID();
    await bindings.DB.prepare(
      `INSERT INTO wallets (
         id, name, initial_balance, currency, icon, user_id, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        walletId,
        "Main Wallet",
        100,
        "USD",
        null,
        statsUserId,
        nowIso(),
        "system",
        otherWalletId,
        "Other Wallet",
        500,
        "USD",
        null,
        otherUserId,
        nowIso(),
        "system"
      )
      .run();

    await bindings.DB.prepare(
      `INSERT INTO transactions (
         id, amount, type, date, note, wallet_id, category_id, user_id, is_transfer, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?), (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        crypto.randomUUID(),
        50,
        "Income",
        formatDateOnlyUtc(monthStart),
        "salary",
        walletId,
        null,
        statsUserId,
        0,
        nowIso(),
        "system",
        crypto.randomUUID(),
        20,
        "Expense",
        formatDateOnlyUtc(addUtcDays(monthStart, 1)),
        "food",
        walletId,
        null,
        statsUserId,
        0,
        nowIso(),
        "system",
        crypto.randomUUID(),
        999,
        "Income",
        formatDateOnlyUtc(addUtcDays(nextMonthStart, -1)),
        "bonus",
        otherWalletId,
        null,
        otherUserId,
        0,
        nowIso(),
        "system",
        crypto.randomUUID(),
        10,
        "Expense",
        "2020-01-15",
        "old",
        walletId,
        null,
        statsUserId,
        0,
        nowIso(),
        "system"
      )
      .run();

    const weekdaysSoFar = getWeekdaysSoFar(today);
    const timeSheetValues: unknown[] = [];
    const timeSheetPlaceholders = weekdaysSoFar
      .map(date => {
        timeSheetValues.push(crypto.randomUUID(), formatDateOnlyUtc(date), "Project A", 8, statsUserId, nowIso(), "system");
        return "(?, ?, ?, ?, ?, ?, ?)";
      })
      .join(", ");

    if (timeSheetPlaceholders) {
      await bindings.DB.prepare(
        `INSERT INTO time_sheets (id, date, project, hours, user_id, created_on, created_by) VALUES ${timeSheetPlaceholders}`
      )
        .bind(...timeSheetValues)
        .run();
    }

    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "stats-user@test.com", password: "Stats@1234" })
    });
    const loginBody = await loginResponse.json() as { accessToken: string };

    const statsResponse = await send(app, bindings, "http://localhost/api/data/stats", {
      headers: { Authorization: `Bearer ${loginBody.accessToken}` }
    });
    expect(statsResponse.status).toBe(200);
    const statsBody = await statsResponse.json() as {
      userCount: number;
      totalBalance: number;
      monthlyIncome: number;
      monthlyExpense: number;
      allLogged: boolean;
      logTimeDetail: string;
    };

    expect(statsBody.userCount).toBe(3);
    expect(statsBody.totalBalance).toBe(120);
    expect(statsBody.monthlyIncome).toBe(50);
    expect(statsBody.monthlyExpense).toBe(20);
    const loggedDatesVisibleToStats = weekdaysSoFar
      .filter(date => date.getUTCFullYear() === today.getUTCFullYear() && date.getUTCMonth() === today.getUTCMonth());

    expect(statsBody.allLogged).toBe(loggedDatesVisibleToStats.length === weekdaysSoFar.length);
    expect(statsBody.logTimeDetail).toBe(`${loggedDatesVisibleToStats.length}/${weekdaysSoFar.length} days this week`);
  });
});

async function getAdminAccessToken(app: ReturnType<typeof createApp>, bindings: AppBindings) {
  const response = await send(app, bindings, "http://localhost/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email: adminEmail, password: adminPassword })
  });
  const body = await response.json() as { accessToken: string };
  return body.accessToken;
}

async function send(app: ReturnType<typeof createApp>, bindings: AppBindings, url: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  return app.fetch(
    new Request(url, { ...init, headers }),
    bindings,
    createExecutionContext()
  );
}

function createExecutionContext() {
  return {
    props: {},
    waitUntil() {},
    passThroughOnException() {}
  };
}

function getCurrentUtcDate() {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
}

function addUtcDays(date: Date, days: number) {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate() + days));
}

function formatDateOnlyUtc(date: Date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function getWeekdaysSoFar(today: Date) {
  const offset = ((today.getUTCDay() - 1 + 7) % 7);
  const monday = addUtcDays(today, -offset);
  const dates: Date[] = [];

  for (let day = monday; day <= today; day = addUtcDays(day, 1)) {
    if (day.getUTCDay() === 0 || day.getUTCDay() === 6) continue;
    dates.push(day);
  }

  return dates;
}
