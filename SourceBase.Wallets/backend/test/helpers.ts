import { SELF } from "cloudflare:test";
import { SignJWT, importJWK } from "jose";
import privateJwk from "./fixtures/test-private-jwk.json";

const ISSUER = "https://test.auth0.local/";
const AUDIENCE = "https://test-api";

export type ApiResult<T = any> = { status: number; body: T };

/** Every test makes its own user (the Auth0 `sub`), so tests never see each other's data. */
export const uniqueUser = (prefix = "user") => `auth0|${prefix}_${crypto.randomUUID().replaceAll("-", "")}`;

export const tokenFor = async (sub: string) => new SignJWT({}).setProtectedHeader({ alg: "RS256", kid: "test" }).setSubject(sub).setIssuer(ISSUER).setAudience(AUDIENCE).setIssuedAt().setExpirationTime("1h").sign(await importJWK(privateJwk, "RS256"));

/** Calls the worker. `user` is the Auth0 sub to authenticate as; pass null for an anonymous request. */
export const api = async <T = any>(user: string | null, method: string, path: string, body?: unknown): Promise<ApiResult<T>> => {
  const headers: Record<string, string> = {};
  if (user) headers.Authorization = `Bearer ${await tokenFor(user)}`;
  if (body !== undefined && !(body instanceof FormData)) headers["Content-Type"] = "application/json";
  const res = await SELF.fetch(`https://example.com/api${path}`, { method, headers, body: body === undefined || body instanceof FormData ? (body as FormData | undefined) : JSON.stringify(body) });
  const text = await res.text();
  return { status: res.status, body: (text ? JSON.parse(text) : undefined) as T };
};

export const NIL_ID = "00000000-0000-0000-0000-000000000000";
export const unknownId = () => crypto.randomUUID();
export const today = () => new Date().toISOString().slice(0, 10);

/** Arrange helpers. Return the created id (or the whole body) and fail loudly if setup fails. */
const must = <T>(res: ApiResult<T>, what: string) => {
  if (res.status !== 200) throw new Error(`${what} failed: ${res.status} ${JSON.stringify(res.body)}`);
  return res.body;
};

export const createWallet = async (user: string, overrides: Record<string, unknown> = {}) => (must(await api<{ id: string }>(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID().replaceAll("-", "")}`, initialBalance: 0, currency: "USD", ...overrides }), "createWallet")).id;
export const createCategory = async (user: string, overrides: Record<string, unknown> = {}) => (must(await api<{ id: string }>(user, "POST", "/categories", { name: `Cat_${crypto.randomUUID().replaceAll("-", "")}`, type: "Expense", ...overrides }), "createCategory")).id;
export const createTransaction = async (user: string, walletId: string, categoryId: string, overrides: Record<string, unknown> = {}) => (must(await api<{ id: string }>(user, "POST", "/transactions", { walletId, categoryId, amount: 100, type: "Expense", date: today(), ...overrides }), "createTransaction")).id;
export const createTransfer = async (user: string, fromWalletId: string, toWalletId: string, overrides: Record<string, unknown> = {}) => (must(await api<{ id: string }>(user, "POST", "/transfers", { fromWalletId, toWalletId, amount: 50, date: today(), ...overrides }), "createTransfer")).id;
export const createIcon = async (user: string, overrides: Record<string, unknown> = {}) => (must(await api<{ id: string }>(user, "POST", "/icons", { value: "🧪", name: `Icon_${crypto.randomUUID().replaceAll("-", "")}`, group: "General", sortOrder: 0, ...overrides }), "createIcon")).id;
export const getWallet = async (user: string, id: string) => must(await api(user, "GET", `/wallets/${id}`), "getWallet");

/** First seeded system category of a type (Income: Salary, Expense: Food & Drink). */
export const systemCategoryId = async (user: string, type: "Income" | "Expense" = "Expense") => (must(await api<{ id: string; isSystem: boolean }[]>(user, "GET", `/categories?type=${type}`), "systemCategory")).find((c) => c.isSystem)!.id;
