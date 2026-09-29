import { createApp } from "../../src/app";
import { bootstrapBaseSchema, seedBaseData } from "../../src/lib/db";
import { FakeD1Database } from "../../src/testing/fake-d1";
import type { AppBindings } from "../../src/types";

const adminEmail = "admin-qt@yopmail.com";
const adminPassword = "pw_test_admin";

export async function setupTestBindings() {
  const bindings: AppBindings = {
    DB: new FakeD1Database(),
    JWT_SECRET: "jwt-secret-for-tests",
    REFRESH_TOKEN_SECRET: "refresh-secret-for-tests",
    ADMIN_EMAIL: adminEmail,
    ADMIN_PASSWORD: adminPassword,
    ACCESS_TOKEN_EXPIRES_IN: "3600",
    REFRESH_TOKEN_EXPIRES_IN: "2592000",
    OTP_TOKEN_EXPIRATION_MINUTES: "15",
    R2_ACCESS_KEY_ID: "test-access-key",
    R2_SECRET_ACCESS_KEY: "test-secret-key",
    R2_BUCKET_NAME: "sourcebase",
    R2_ACCOUNT_ID: "account-123",
    R2_PUBLIC_URL: "https://cdn.sourcebase.test",
    R2_UPLOAD_EXPIRES_IN: "600"
  };

  await bootstrapBaseSchema(bindings.DB);
  await seedBaseData(bindings.DB, bindings);
  return bindings;
}

export async function createVerifiedUser(app: ReturnType<typeof createApp>, bindings: AppBindings, seed = crypto.randomUUID()) {
  const email = `user_${seed}@test.com`;
  const userName = `user_${seed.replaceAll("-", "")}`;
  const password = "Test@1234!";

  const registerResponse = await send(app, bindings, "http://localhost/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ userName, email, password })
  });

  if (registerResponse.status !== 200) throw new Error(`Failed to register test user: ${registerResponse.status}`);

  const otpRow = await bindings.DB.prepare(`SELECT otp_code FROM users WHERE lower(email) = lower(?)`)
    .bind(email)
    .first<{ otp_code: string }>();

  const confirmResponse = await send(app, bindings, "http://localhost/api/auth/confirmEmail", {
    method: "POST",
    body: JSON.stringify({ email, code: otpRow?.otp_code })
  });

  if (confirmResponse.status !== 200) throw new Error(`Failed to confirm test user: ${confirmResponse.status}`);

  const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });

  if (loginResponse.status !== 200) throw new Error(`Failed to login test user: ${loginResponse.status}`);

  const loginBody = await loginResponse.json() as { accessToken: string };
  return { email, userName, password, accessToken: loginBody.accessToken };
}

export async function getUserInfo(app: ReturnType<typeof createApp>, bindings: AppBindings, accessToken: string) {
  const response = await send(app, bindings, "http://localhost/api/auth/info", {
    headers: authHeaders(accessToken)
  });

  if (response.status !== 200) throw new Error(`Failed to get user info: ${response.status}`);
  return response.json() as Promise<{ id: string; userName: string; defaultTodoListId: string | null }>;
}

export function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

export async function send(app: ReturnType<typeof createApp>, bindings: AppBindings, url: string, init: RequestInit = {}) {
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
