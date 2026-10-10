import { describe, expect, it } from "vitest";
import { api, createWallet, systemCategoryId, uniqueUser, unknownId } from "../../helpers";

describe("GetWallet", () => {
  it("WALLETS-GET-001: without token returns 401", async () => {
    const res = await api(null, "GET", `/wallets/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("WALLETS-GET-002: owned wallet id returns wallet data", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { name: `Travel_${crypto.randomUUID()}`, initialBalance: 200, currency: "GBP", icon: "✈️" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    await api(user, "POST", "/transactions", { walletId: id, amount: 50, type: "Income", date: "2025-01-12", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });

    const res = await api(user, "GET", `/wallets/${id}`);

    expect(res.status).toBe(200);
    expect(res.body.id).toBe(id);
    expect(res.body.currency).toBe("GBP");
    expect(res.body.icon).toBe("✈️");
    expect(res.body.balance).toBe(250);
  });

  it("WALLETS-GET-003: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "GET", `/wallets/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("WALLETS-GET-004: other user's wallet returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const id = await createWallet(owner, { initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(other, "GET", `/wallets/${id}`);

    expect(res.status).toBe(404);
  });

  it("WALLETS-GET-005: balance accurately reflects multiple transactions", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 100, currency: "USD", icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    await api(user, "POST", "/transactions", { walletId: id, amount: 50, type: "Income", date: "2025-01-01", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });
    await api(user, "POST", "/transactions", { walletId: id, amount: 30, type: "Expense", date: "2025-01-02", note: `Txn_${crypto.randomUUID()}`, categoryId: expenseCategoryId });
    await api(user, "POST", "/transactions", { walletId: id, amount: 20, type: "Income", date: "2025-01-03", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });

    const res = await api(user, "GET", `/wallets/${id}`);

    expect(res.status).toBe(200);
    expect(res.body.balance).toBe(140); // 100 + 50 - 30 + 20
  });
});
