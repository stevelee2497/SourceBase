import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import { bootstrapBaseSchema, seedBaseData } from "../src/lib/db";
import { FakeD1Database } from "../src/testing/fake-d1";
import type { AppBindings } from "../src/types";

const adminEmail = "admin-qt@yopmail.com";
const adminPassword = "pw_test_admin";

describe("SourceBase.Workers auth routes", () => {
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

  it("register + confirm + login returns tokens", async () => {
    const app = createApp();
    const email = `user_${crypto.randomUUID()}@test.com`;
    const userName = `user_${crypto.randomUUID().replaceAll("-", "")}`;
    const password = "Test@1234!";

    const registerResponse = await send(app, bindings, "http://localhost/api/auth/register", {
      method: "POST",
      body: JSON.stringify({
        userName: `  ${userName}  `,
        email: `  ${email}  `,
        password: `  ${password}  `
      })
    });

    expect(registerResponse.status).toBe(200);

    const otpRow = await bindings.DB.prepare(`SELECT otp_code FROM users WHERE lower(email) = lower(?)`)
      .bind(email)
      .first<{ otp_code: string }>();

    const confirmResponse = await send(app, bindings, "http://localhost/api/auth/confirmEmail", {
      method: "POST",
      body: JSON.stringify({ email, code: otpRow?.otp_code })
    });
    expect(confirmResponse.status).toBe(200);

    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    });
    expect(loginResponse.status).toBe(200);
    const loginBody = await loginResponse.json() as {
      accessToken: string;
      refreshToken: string;
      expiresIn: number;
      tokenType: string;
    };

    expect(loginBody.tokenType).toBe("Bearer");
    expect(loginBody.accessToken).toBeTruthy();
    expect(loginBody.refreshToken).toBeTruthy();
    expect(loginBody.expiresIn).toBe(3600);
  });

  it("logout invalidates existing access token", async () => {
    const app = createApp();
    const token = await getAdminAccessToken(app, bindings);

    const logoutResponse = await send(app, bindings, "http://localhost/api/auth/logout", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(logoutResponse.status).toBe(200);

    const getInfoResponse = await send(app, bindings, "http://localhost/api/auth/info", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(getInfoResponse.status).toBe(401);
  });

  it("refresh returns new tokens and preserves admin role", async () => {
    const app = createApp();
    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: adminEmail, password: adminPassword })
    });
    const loginBody = await loginResponse.json() as { refreshToken: string };

    const refreshResponse = await send(app, bindings, "http://localhost/api/auth/refresh", {
      method: "POST",
      body: JSON.stringify({ token: loginBody.refreshToken })
    });

    expect(refreshResponse.status).toBe(200);
    const refreshBody = await refreshResponse.json() as { accessToken: string; refreshToken: string };
    expect(refreshBody.accessToken).toBeTruthy();
    expect(refreshBody.refreshToken).toBeTruthy();

    const getInfoResponse = await send(app, bindings, "http://localhost/api/auth/info", {
      headers: { Authorization: `Bearer ${refreshBody.accessToken}` }
    });
    const infoBody = await getInfoResponse.json() as { roles: string[] };
    expect(infoBody.roles).toContain("Admin");
  });

  it("update info keeps null fields as existing values", async () => {
    const app = createApp();
    const token = await getAdminAccessToken(app, bindings);

    const firstUpdate = await send(app, bindings, "http://localhost/api/auth/info", {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ phoneNumber: "0111222333" })
    });
    expect(firstUpdate.status).toBe(200);

    const secondUpdate = await send(app, bindings, "http://localhost/api/auth/info", {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ firstName: "Admin", phoneNumber: null })
    });
    expect(secondUpdate.status).toBe(200);

    const getInfoResponse = await send(app, bindings, "http://localhost/api/auth/info", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const infoBody = await getInfoResponse.json() as { firstName: string; phoneNumber: string };
    expect(infoBody.firstName).toBe("Admin");
    expect(infoBody.phoneNumber).toBe("0111222333");
  });

  it("forgot + reset password invalidates old refresh token", async () => {
    const app = createApp();
    const email = `reset_${crypto.randomUUID()}@test.com`;
    const userName = `reset_${crypto.randomUUID().replaceAll("-", "")}`;
    const password = "Test@1234!";

    await send(app, bindings, "http://localhost/api/auth/register", {
      method: "POST",
      body: JSON.stringify({ userName, email, password })
    });

    const confirmOtp = await bindings.DB.prepare(`SELECT otp_code FROM users WHERE lower(email) = lower(?)`)
      .bind(email)
      .first<{ otp_code: string }>();
    await send(app, bindings, "http://localhost/api/auth/confirmEmail", {
      method: "POST",
      body: JSON.stringify({ email, code: confirmOtp?.otp_code })
    });

    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    });
    const loginBody = await loginResponse.json() as { refreshToken: string };

    const forgotResponse = await send(app, bindings, "http://localhost/api/auth/forgotPassword", {
      method: "POST",
      body: JSON.stringify({ email })
    });
    expect(forgotResponse.status).toBe(200);

    const resetOtp = await bindings.DB.prepare(`SELECT otp_code FROM users WHERE lower(email) = lower(?)`)
      .bind(email)
      .first<{ otp_code: string }>();
    const resetResponse = await send(app, bindings, "http://localhost/api/auth/resetPassword", {
      method: "POST",
      body: JSON.stringify({ email, code: resetOtp?.otp_code, newPassword: "NewTest@5678!" })
    });
    expect(resetResponse.status).toBe(200);

    const refreshResponse = await send(app, bindings, "http://localhost/api/auth/refresh", {
      method: "POST",
      body: JSON.stringify({ token: loginBody.refreshToken })
    });
    expect(refreshResponse.status).toBe(401);
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
