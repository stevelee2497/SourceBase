import { createApp } from "../src/app";
import { bootstrapBaseSchema, seedBaseData } from "../src/lib/db";
import { FakeD1Database } from "../src/testing/fake-d1";
import type { AppBindings } from "../src/types";

const adminEmail = "admin-qt@yopmail.com";
const adminPassword = "pw_test_admin";

export type TestContext = {
  app: ReturnType<typeof createApp>;
  bindings: AppBindings;
};

export async function createTestContext(): Promise<TestContext> {
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

  return {
    app: createApp(),
    bindings
  };
}

export async function send(context: TestContext, url: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  return context.app.fetch(new Request(url, { ...init, headers }), context.bindings, createExecutionContext());
}

export async function getAdminAccessToken(context: TestContext) {
  const response = await send(context, "http://localhost/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email: adminEmail, password: adminPassword })
  });
  const body = await response.json() as { accessToken: string };
  return body.accessToken;
}

export async function createUserAndGetAccessToken(context: TestContext) {
  const email = `user_${crypto.randomUUID()}@test.com`;
  const userName = `user_${crypto.randomUUID().replaceAll("-", "")}`;
  const password = "Test@1234!";

  await send(context, "http://localhost/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ userName, email, password })
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

export async function getUserByEmail(context: TestContext, email: string) {
  return context.bindings.DB.prepare(
    `SELECT id, user_name, email
     FROM users
     WHERE lower(email) = lower(?)`
  )
    .bind(email)
    .first<{ id: string; user_name: string; email: string }>();
}

export function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

export async function createWallet(context: TestContext, token: string, body: { name: string; initialBalance: number; currency: string; icon?: string | null }) {
  const response = await send(context, "http://localhost/api/wallets", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify(body)
  });
  return {
    response,
    body: await response.json() as { id: string }
  };
}

export async function createCategory(context: TestContext, token: string, body: { name: string; type: "Income" | "Expense"; icon?: string | null }) {
  const response = await send(context, "http://localhost/api/categories", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify(body)
  });
  return {
    response,
    body: await response.json() as { id: string }
  };
}

export async function createTransaction(
  context: TestContext,
  token: string,
  body: { walletId: string; amount: number; type: "Income" | "Expense"; date: string; note?: string | null; categoryId: string }
) {
  const response = await send(context, "http://localhost/api/transactions", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify(body)
  });
  return {
    response,
    body: await response.json() as { id: string }
  };
}

function createExecutionContext() {
  return {
    props: {},
    waitUntil() {},
    passThroughOnException() {}
  };
}
