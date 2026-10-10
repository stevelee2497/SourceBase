import { env } from "cloudflare:test";
import { describe, expect, it } from "vitest";
import { api, NIL_ID, uniqueUser } from "../../helpers";

describe("CreateWallet", () => {
  it("WALLETS-CREATE-001: missing token returns 401", async () => {
    const res = await api(null, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 100, currency: "USD" });
    expect(res.status).toBe(401);
  });

  it("WALLETS-CREATE-002: valid data returns 200 and wallet id", async () => {
    const user = uniqueUser();
    const res = await api(user, "POST", "/wallets", { name: `Primary_${crypto.randomUUID()}`, initialBalance: 100, currency: "USD", icon: "💳" });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
    expect(res.body.id).not.toBe(NIL_ID);
  });

  it("WALLETS-CREATE-003: missing name returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/wallets", { initialBalance: 100, currency: "USD" });
    expect(res.status).toBe(400);
  });

  it("WALLETS-CREATE-004: missing currency returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/wallets", { name: `NoCurrency_${crypto.randomUUID()}`, initialBalance: 100 });
    expect(res.status).toBe(400);
  });

  it("WALLETS-CREATE-005: negative initial balance returns 200", async () => {
    const user = uniqueUser();
    const create = await api(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: -25, currency: "USD", icon: "💳" });
    expect(create.status).toBe(200);

    const wallet = await api(user, "GET", `/wallets/${create.body.id}`);
    expect(wallet.status).toBe(200);
    expect(wallet.body.balance).toBe(-25);
    expect(wallet.body.initialBalance).toBe(-25);
  });

  it("WALLETS-CREATE-006: balance equals initial balance when no transactions", async () => {
    const user = uniqueUser();
    const create = await api(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 123.45, currency: "EUR", icon: "💳" });
    expect(create.status).toBe(200);

    const wallet = await api(user, "GET", `/wallets/${create.body.id}`);
    expect(wallet.status).toBe(200);
    expect(wallet.body.balance).toBe(123.45);
    expect(wallet.body.currency).toBe("EUR");
  });

  it("WALLETS-CREATE-007: authenticated user sets wallet ownership", async () => {
    const user = uniqueUser("wallet_owner");
    const create = await api(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 0, currency: "USD", icon: "💳" });
    expect(create.status).toBe(200);

    const row = await env.DB.prepare("SELECT user_id FROM wallets WHERE id = ?").bind(create.body.id).first<{ user_id: string }>();
    expect(row!.user_id).toBe(user);
  });

  it("WALLETS-CREATE-008: zero initial balance results in zero balance", async () => {
    const user = uniqueUser();
    const create = await api(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 0, currency: "USD", icon: "💳" });
    expect(create.status).toBe(200);

    const wallet = await api(user, "GET", `/wallets/${create.body.id}`);
    expect(wallet.status).toBe(200);
    expect(wallet.body.balance).toBe(0);
    expect(wallet.body.initialBalance).toBe(0);
  });

  it("WALLETS-CREATE-009: missing icon still creates wallet", async () => {
    const user = uniqueUser();
    const create = await api(user, "POST", "/wallets", { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 50, currency: "USD" });
    expect(create.status).toBe(200);

    const wallet = await api(user, "GET", `/wallets/${create.body.id}`);
    expect(wallet.status).toBe(200);
    expect(wallet.body.id).toBe(create.body.id);
    expect(wallet.body.balance).toBe(50);
  });
});
