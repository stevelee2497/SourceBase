import { describe, expect, it } from "vitest";
import { api, createTransaction, createTransfer, createWallet, getWallet, systemCategoryId, NIL_ID, unknownId, uniqueUser } from "../../helpers";

const note = () => `Txn_${crypto.randomUUID().replaceAll("-", "")}`;

describe("UpdateTransaction", () => {
  it("TXN-UPDATE-001: no token returns 401", async () => {
    const res = await api(null, "PATCH", `/transactions/${unknownId()}`, { amount: 50, type: "Income", date: "2025-01-22", categoryId: crypto.randomUUID() });
    expect(res.status).toBe(401);
  });

  it("TXN-UPDATE-002: valid data returns 200", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const originalCategoryId = await systemCategoryId(user, "Income");
    const updatedCategoryId = await systemCategoryId(user, "Expense");
    const id = await createTransaction(user, walletId, originalCategoryId, { amount: 20, type: "Income", date: "2025-01-23", note: "Original note" });
    const res = await api(user, "PATCH", `/transactions/${id}`, { amount: 55, type: "Expense", date: "2025-02-02", note: "Updated note", categoryId: updatedCategoryId });
    expect(res.status).toBe(200);
    expect(res.body.id).toBe(id);
    const updated = (await api(user, "GET", `/transactions/${id}`)).body;
    expect(updated.amount).toBe(55);
    expect(updated.type).toBe("Expense");
    expect(updated.date).toBe("2025-02-02");
    expect(updated.note).toBe("Updated note");
    expect(updated.categoryId).toBe(updatedCategoryId);
  });

  it("TXN-UPDATE-003: amount change recomputes wallet balance", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const id = await createTransaction(user, walletId, categoryId, { amount: 10, type: "Income", date: "2025-01-24", note: note() });
    const res = await api(user, "PATCH", `/transactions/${id}`, { amount: 40, type: "Income", date: "2025-01-24", note: "Adjusted", categoryId });
    expect(res.status).toBe(200);
    expect((await getWallet(user, walletId)).balance).toBe(140);
  });

  it("TXN-UPDATE-004: type change recomputes wallet balance", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    const id = await createTransaction(user, walletId, incomeCategoryId, { amount: 30, type: "Income", date: "2025-01-25", note: note() });
    const res = await api(user, "PATCH", `/transactions/${id}`, { amount: 30, type: "Expense", date: "2025-01-25", note: "Converted to expense", categoryId: expenseCategoryId });
    expect(res.status).toBe(200);
    expect((await getWallet(user, walletId)).balance).toBe(70);
  });

  it("TXN-UPDATE-005: zero or negative amount returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const id = await createTransaction(user, walletId, categoryId, { amount: 30, type: "Income", date: "2025-01-26", note: note() });
    const zero = await api(user, "PATCH", `/transactions/${id}`, { amount: 0, type: "Income", date: "2025-01-26", note: "Zero", categoryId });
    const negative = await api(user, "PATCH", `/transactions/${id}`, { amount: -5, type: "Income", date: "2025-01-26", note: "Negative", categoryId });
    expect(zero.status).toBe(400);
    expect(negative.status).toBe(400);
  });

  it("TXN-UPDATE-006: unknown id returns 404", async () => {
    const user = uniqueUser();
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "PATCH", `/transactions/${unknownId()}`, { amount: 30, type: "Income", date: "2025-01-27", categoryId });
    expect(res.status).toBe(404);
  });

  it("TXN-UPDATE-007: other user's transaction returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const walletId = await createWallet(owner);
    const id = await createTransaction(owner, walletId, await systemCategoryId(owner, "Income"), { amount: 20, type: "Income", date: "2025-01-28", note: note() });
    const otherCategoryId = await systemCategoryId(other, "Income");
    const res = await api(other, "PATCH", `/transactions/${id}`, { amount: 50, type: "Income", date: "2025-01-28", categoryId: otherCategoryId });
    expect(res.status).toBe(404);
  });

  it("TXN-UPDATE-008: transfer transaction returns 400", async () => {
    const user = uniqueUser();
    const fromWalletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const toWalletId = await createWallet(user, { initialBalance: 50, icon: "💳" });
    await createTransfer(user, fromWalletId, toWalletId, { amount: 30, date: "2025-01-29", note: `Transfer_${crypto.randomUUID()}` });
    const list = await api(user, "GET", `/transactions?walletIds=${fromWalletId}&limit=100`);
    const transfers = list.body.items.filter((x: any) => x.isTransfer);
    expect(transfers).toHaveLength(1);
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    const res = await api(user, "PATCH", `/transactions/${transfers[0].id}`, { amount: 35, type: "Expense", date: "2025-01-29", note: "Updated transfer", categoryId: expenseCategoryId });
    expect(res.status).toBe(400);
    expect(JSON.stringify(res.body)).toContain("Transfer transactions cannot be edited directly");
  });

  it("TXN-UPDATE-009: wallet id moves transaction and recalculates balances", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const walletB = await createWallet(user, { initialBalance: 50, icon: "💳" });
    const id = await createTransaction(user, walletA, await systemCategoryId(user, "Income"), { amount: 30, type: "Income", date: "2025-03-01", note: note() });
    const res = await api(user, "PATCH", `/transactions/${id}`, { walletId: walletB });
    expect(res.status).toBe(200);
    expect((await getWallet(user, walletA)).balance).toBe(100);
    expect((await getWallet(user, walletB)).balance).toBe(80);
    expect((await api(user, "GET", `/transactions/${id}`)).body.walletId).toBe(walletB);
  });

  it("TXN-UPDATE-010: invalid wallet id returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const id = await createTransaction(user, walletId, await systemCategoryId(user, "Income"), { amount: 20, type: "Income", date: "2025-03-02", note: note() });
    const res = await api(user, "PATCH", `/transactions/${id}`, { walletId: unknownId() });
    expect(res.status).toBe(400);
  });

  it("TXN-UPDATE-011: other user's wallet id returns 400", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const ownerWalletId = await createWallet(owner, { icon: "💳" });
    const otherWalletId = await createWallet(other, { icon: "💳" });
    const id = await createTransaction(owner, ownerWalletId, await systemCategoryId(owner, "Income"), { amount: 20, type: "Income", date: "2025-03-03", note: note() });
    const res = await api(owner, "PATCH", `/transactions/${id}`, { walletId: otherWalletId });
    expect(res.status).toBe(400);
  });

  it("TXN-UPDATE-012: empty id returns 400", async () => {
    const res = await api(uniqueUser(), "PATCH", `/transactions/${NIL_ID}`, { amount: 10 });
    expect(res.status).toBe(400);
  });
});
