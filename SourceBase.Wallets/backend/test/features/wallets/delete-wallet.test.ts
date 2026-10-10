import { describe, expect, it } from "vitest";
import { api, createWallet, systemCategoryId, uniqueUser, unknownId } from "../../helpers";

describe("DeleteWallet", () => {
  it("WALLETS-DELETE-001: no token returns 401", async () => {
    const res = await api(null, "DELETE", `/wallets/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("WALLETS-DELETE-002: owned wallet returns 200", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(user, "DELETE", `/wallets/${id}`);

    expect(res.status).toBe(200);
    expect(res.body.success).toBe(true);
    expect((await api(user, "GET", `/wallets/${id}`)).status).toBe(404);
  });

  it("WALLETS-DELETE-003: other user's wallet returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const id = await createWallet(owner, { initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(other, "DELETE", `/wallets/${id}`);

    expect(res.status).toBe(404);
  });

  it("WALLETS-DELETE-004: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "DELETE", `/wallets/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("WALLETS-DELETE-005: delete removes associated transactions", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 100, currency: "USD", icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    await api(user, "POST", "/transactions", { walletId: id, amount: 20, type: "Income", date: "2025-01-14", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });
    await api(user, "POST", "/transactions", { walletId: id, amount: 5, type: "Expense", date: "2025-01-15", note: `Txn_${crypto.randomUUID()}`, categoryId: expenseCategoryId });

    const res = await api(user, "DELETE", `/wallets/${id}`);

    expect(res.status).toBe(200);
    expect((await api(user, "GET", `/wallets/${id}`)).status).toBe(404);
    const txns = await api(user, "GET", `/transactions?walletIds=${id}&limit=100`);
    expect(txns.body.total).toBe(0);
  });

  it("WALLETS-DELETE-006: deleted wallet is excluded from list", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 0, currency: "USD", icon: "💳" });
    await api(user, "DELETE", `/wallets/${id}`);

    const res = await api(user, "GET", "/wallets");

    expect(res.status).toBe(200);
    expect(res.body.wallets.some((w: { id: string }) => w.id === id)).toBe(false);
  });
});
