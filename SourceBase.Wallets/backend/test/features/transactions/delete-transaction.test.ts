import { describe, expect, it } from "vitest";
import { api, createTransaction, createTransfer, createWallet, getWallet, systemCategoryId, unknownId, uniqueUser } from "../../helpers";

describe("DeleteTransaction", () => {
  it("TXN-DELETE-001: missing token returns 401", async () => {
    const res = await api(null, "DELETE", `/transactions/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("TXN-DELETE-002: income transaction deletion recomputes wallet balance", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const id = await createTransaction(user, walletId, categoryId, { amount: 25, type: "Income", date: "2025-01-30", note: `Txn_${crypto.randomUUID()}` });
    const res = await api(user, "DELETE", `/transactions/${id}`);
    expect(res.status).toBe(200);
    expect((await getWallet(user, walletId)).balance).toBe(100);
  });

  it("TXN-DELETE-003: expense transaction deletion recomputes wallet balance", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const categoryId = await systemCategoryId(user, "Expense");
    const id = await createTransaction(user, walletId, categoryId, { amount: 25, type: "Expense", date: "2025-01-31", note: `Txn_${crypto.randomUUID()}` });
    const res = await api(user, "DELETE", `/transactions/${id}`);
    expect(res.status).toBe(200);
    expect((await getWallet(user, walletId)).balance).toBe(100);
  });

  it("TXN-DELETE-004: transfer transaction deletion returns 400", async () => {
    const user = uniqueUser();
    const fromWalletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const toWalletId = await createWallet(user, { initialBalance: 50, icon: "💳" });
    await createTransfer(user, fromWalletId, toWalletId, { amount: 30, date: "2025-02-01", note: `Transfer_${crypto.randomUUID()}` });
    const list = await api(user, "GET", `/transactions?walletIds=${fromWalletId}&limit=100`);
    const transferTransactionId = list.body.items.filter((x: any) => x.isTransfer)[0].id;
    expect(list.body.items.filter((x: any) => x.isTransfer)).toHaveLength(1);
    const res = await api(user, "DELETE", `/transactions/${transferTransactionId}`);
    expect(res.status).toBe(400);
    expect(JSON.stringify(res.body)).toContain("Transfer transactions cannot be deleted directly");
  });

  it("TXN-DELETE-005: unknown transaction id returns 404", async () => {
    const res = await api(uniqueUser(), "DELETE", `/transactions/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("TXN-DELETE-006: other user's transaction returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const walletId = await createWallet(owner);
    const categoryId = await systemCategoryId(owner, "Income");
    const id = await createTransaction(owner, walletId, categoryId, { amount: 25, type: "Income", date: "2025-02-02", note: `Txn_${crypto.randomUUID()}` });
    const res = await api(other, "DELETE", `/transactions/${id}`);
    expect(res.status).toBe(404);
  });
});
