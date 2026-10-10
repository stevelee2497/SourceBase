import { describe, expect, it } from "vitest";
import { api, createWallet, systemCategoryId, uniqueUser } from "../../helpers";

type WalletDto = { id: string; currency: string; icon?: string };

describe("GetWallets", () => {
  it("WALLETS-GET-ALL-001: without token returns 401", async () => {
    const res = await api(null, "GET", "/wallets");
    expect(res.status).toBe(401);
  });

  it("WALLETS-GET-ALL-002: owned wallets return 200", async () => {
    const user = uniqueUser();
    const first = await createWallet(user, { name: `Cash_${crypto.randomUUID()}`, initialBalance: 50, currency: "USD", icon: "💳" });
    const second = await createWallet(user, { name: `Bank_${crypto.randomUUID()}`, initialBalance: 75, currency: "USD", icon: "🏦" });

    const res = await api(user, "GET", "/wallets");

    expect(res.status).toBe(200);
    const ids = res.body.wallets.map((w: WalletDto) => w.id);
    expect(ids).toContain(first);
    expect(ids).toContain(second);
  });

  it("WALLETS-GET-ALL-003: multiple users returns only current user's wallets", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const own = await createWallet(owner, { name: `Own_${crypto.randomUUID()}`, initialBalance: 10, currency: "USD", icon: "💳" });
    const foreign = await createWallet(other, { name: `Other_${crypto.randomUUID()}`, initialBalance: 20, currency: "USD", icon: "💳" });

    const res = await api(owner, "GET", "/wallets");

    expect(res.status).toBe(200);
    const ids = res.body.wallets.map((w: WalletDto) => w.id);
    expect(ids).toContain(own);
    expect(ids).not.toContain(foreign);
  });

  it("WALLETS-GET-ALL-004: balances returns correct total balance", async () => {
    const user = uniqueUser();
    const first = await createWallet(user, { name: `Main_${crypto.randomUUID()}`, initialBalance: 100, currency: "USD", icon: "💳" });
    const second = await createWallet(user, { name: `Savings_${crypto.randomUUID()}`, initialBalance: 50, currency: "USD", icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    await api(user, "POST", "/transactions", { walletId: first, amount: 25, type: "Income", date: "2025-01-10", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });
    await api(user, "POST", "/transactions", { walletId: second, amount: 10, type: "Expense", date: "2025-01-11", note: `Txn_${crypto.randomUUID()}`, categoryId: expenseCategoryId });

    const res = await api(user, "GET", "/wallets");

    expect(res.status).toBe(200);
    expect(res.body.totalBalance).toBe(165);
  });

  it("WALLETS-GET-ALL-005: no wallets returns empty list", async () => {
    const res = await api(uniqueUser(), "GET", "/wallets");

    expect(res.status).toBe(200);
    expect(res.body.wallets).toEqual([]);
    expect(res.body.totalBalance).toBe(0);
  });

  it("WALLETS-GET-ALL-006: returns wallet currency and icon", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { name: `Wallet_${crypto.randomUUID()}`, initialBalance: 0, currency: "EUR", icon: "✈️" });

    const res = await api(user, "GET", "/wallets");

    expect(res.status).toBe(200);
    const matches = res.body.wallets.filter((w: WalletDto) => w.id === id);
    expect(matches).toHaveLength(1);
    expect(matches[0].currency).toBe("EUR");
    expect(matches[0].icon).toBe("✈️");
  });
});
