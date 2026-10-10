import { describe, expect, it } from "vitest";
import { api, createCategory, createTransaction, createTransfer, createWallet, systemCategoryId, uniqueUser } from "../../helpers";

const note = () => `Txn_${crypto.randomUUID().replaceAll("-", "")}`;

describe("GetTransactions", () => {
  it("TXN-GET-ALL-001: missing token returns 401", async () => {
    const res = await api(null, "GET", "/transactions");
    expect(res.status).toBe(401);
  });

  it("TXN-GET-ALL-002: owned transactions return 200", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const id = await createTransaction(user, walletId, categoryId, { amount: 20, type: "Income", date: "2025-01-10", note: note() });
    const res = await api(user, "GET", "/transactions?limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === id)).toBe(true);
    expect(res.body.total).toBe(1);
  });

  it("TXN-GET-ALL-003: multiple users returns only current user's transactions", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const ownerWalletId = await createWallet(owner, { icon: "💳" });
    const otherWalletId = await createWallet(other, { icon: "💳" });
    const ownId = await createTransaction(owner, ownerWalletId, await systemCategoryId(owner, "Income"), { amount: 20, type: "Income", date: "2025-01-11", note: note() });
    const otherId = await createTransaction(other, otherWalletId, await systemCategoryId(other, "Income"), { amount: 15, type: "Income", date: "2025-01-11", note: note() });
    const res = await api(owner, "GET", "/transactions?limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === ownId)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === otherId)).toBe(false);
  });

  it("TXN-GET-ALL-004: wallet filter returns matching transactions", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const idA = await createTransaction(user, walletA, categoryId, { amount: 10, type: "Income", date: "2025-01-12", note: note() });
    await createTransaction(user, walletB, categoryId, { amount: 20, type: "Income", date: "2025-01-12", note: note() });
    const res = await api(user, "GET", `/transactions?walletIds=${walletA}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === idA && x.walletId === walletA)).toBe(true);
  });

  it("TXN-GET-ALL-005: income type filter returns only income transactions", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    await createTransaction(user, walletId, await systemCategoryId(user, "Income"), { amount: 10, type: "Income", date: "2025-01-13", note: note() });
    await createTransaction(user, walletId, await systemCategoryId(user, "Expense"), { amount: 5, type: "Expense", date: "2025-01-13", note: note() });
    const res = await api(user, "GET", "/transactions?type=Income&limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.length).toBeGreaterThan(0);
    expect(res.body.items.every((x: any) => x.type === "Income")).toBe(true);
  });

  it("TXN-GET-ALL-006: expense type filter returns only expense transactions", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    await createTransaction(user, walletId, await systemCategoryId(user, "Income"), { amount: 10, type: "Income", date: "2025-01-14", note: note() });
    await createTransaction(user, walletId, await systemCategoryId(user, "Expense"), { amount: 5, type: "Expense", date: "2025-01-14", note: note() });
    const res = await api(user, "GET", "/transactions?type=Expense&limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.length).toBeGreaterThan(0);
    expect(res.body.items.every((x: any) => x.type === "Expense")).toBe(true);
  });

  it("TXN-GET-ALL-007: date range filter returns transactions within range", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    await createTransaction(user, walletId, categoryId, { amount: 10, type: "Income", date: "2025-01-01", note: note() });
    const inRangeId = await createTransaction(user, walletId, categoryId, { amount: 15, type: "Income", date: "2025-01-15", note: note() });
    await createTransaction(user, walletId, categoryId, { amount: 20, type: "Income", date: "2025-02-01", note: note() });
    const res = await api(user, "GET", "/transactions?dateFrom=2025-01-10&dateTo=2025-01-20&limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === inRangeId)).toBe(true);
  });

  it("TXN-GET-ALL-008: category filter returns matching transactions", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const firstCategory = await createCategory(user, { type: "Expense", icon: "🏷️" });
    const secondCategory = await createCategory(user, { type: "Expense", icon: "🏷️" });
    const matchingId = await createTransaction(user, walletId, firstCategory, { amount: 10, type: "Expense", date: "2025-01-16", note: note() });
    await createTransaction(user, walletId, secondCategory, { amount: 5, type: "Expense", date: "2025-01-16", note: note() });
    const res = await api(user, "GET", `/transactions?categoryIds=${firstCategory}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === matchingId && x.categoryId === firstCategory)).toBe(true);
  });

  it("TXN-GET-ALL-009: multiple category ids filter returns transactions in any of them", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const firstCategory = await createCategory(user, { type: "Expense", icon: "🏷️" });
    const secondCategory = await createCategory(user, { type: "Expense", icon: "🏷️" });
    const thirdCategory = await createCategory(user, { type: "Expense", icon: "🏷️" });
    const firstId = await createTransaction(user, walletId, firstCategory, { amount: 10, type: "Expense", date: "2025-01-16", note: note() });
    const secondId = await createTransaction(user, walletId, secondCategory, { amount: 5, type: "Expense", date: "2025-01-16", note: note() });
    const excludedId = await createTransaction(user, walletId, thirdCategory, { amount: 7, type: "Expense", date: "2025-01-16", note: note() });
    const res = await api(user, "GET", `/transactions?categoryIds=${firstCategory}&categoryIds=${secondCategory}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === firstId)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === secondId)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === excludedId)).toBe(false);
  });

  it("TXN-GET-ALL-010: single wallet ids filter returns matching transactions", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const idA = await createTransaction(user, walletA, categoryId, { amount: 10, type: "Income", date: "2025-02-01", note: note() });
    await createTransaction(user, walletB, categoryId, { amount: 20, type: "Income", date: "2025-02-01", note: note() });
    const res = await api(user, "GET", `/transactions?walletIds=${walletA}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === idA && x.walletId === walletA)).toBe(true);
  });

  it("TXN-GET-ALL-011: multiple wallet ids filter returns transactions from all specified wallets", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    const walletC = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const idA = await createTransaction(user, walletA, categoryId, { amount: 10, type: "Income", date: "2025-02-02", note: note() });
    const idB = await createTransaction(user, walletB, categoryId, { amount: 20, type: "Income", date: "2025-02-02", note: note() });
    await createTransaction(user, walletC, categoryId, { amount: 30, type: "Income", date: "2025-02-02", note: note() });
    const res = await api(user, "GET", `/transactions?walletIds=${walletA}&walletIds=${walletB}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.total).toBe(2);
    expect(res.body.items.some((x: any) => x.id === idA && x.walletId === walletA)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === idB && x.walletId === walletB)).toBe(true);
    expect(res.body.items.some((x: any) => x.walletId === walletC)).toBe(false);
  });

  it("TXN-GET-ALL-012: wallet ids filter combined with type filter returns matching transactions", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    const incomeId = await createTransaction(user, walletA, incomeCategoryId, { amount: 10, type: "Income", date: "2025-02-03", note: note() });
    await createTransaction(user, walletA, expenseCategoryId, { amount: 5, type: "Expense", date: "2025-02-03", note: note() });
    await createTransaction(user, walletB, incomeCategoryId, { amount: 15, type: "Income", date: "2025-02-03", note: note() });
    const res = await api(user, "GET", `/transactions?walletIds=${walletA}&type=Income&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === incomeId)).toBe(true);
    expect(res.body.items.every((x: any) => x.walletId === walletA && x.type === "Income")).toBe(true);
  });

  it("TXN-GET-ALL-009: pagination returns correct subset", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    await createTransaction(user, walletId, categoryId, { amount: 10, type: "Income", date: "2025-01-01", note: note() });
    const secondId = await createTransaction(user, walletId, categoryId, { amount: 20, type: "Income", date: "2025-01-02", note: note() });
    await createTransaction(user, walletId, categoryId, { amount: 30, type: "Income", date: "2025-01-03", note: note() });
    const res = await api(user, "GET", "/transactions?page=2&limit=1");
    expect(res.status).toBe(200);
    expect(res.body.total).toBe(3);
    expect(res.body.items).toHaveLength(1);
    expect(res.body.items[0].id).toBe(secondId);
  });

  it("TXN-GET-ALL-013: exclude=Transfer hides transfer transactions", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const walletB = await createWallet(user, { initialBalance: 0, icon: "💳" });
    const txnId = await createTransaction(user, walletA, await systemCategoryId(user, "Income"), { amount: 50, type: "Income", date: "2025-03-01", note: note() });
    await createTransfer(user, walletA, walletB, { amount: 30, date: "2025-03-02" });
    const res = await api(user, "GET", "/transactions?limit=50&exclude=Transfer");
    expect(res.status).toBe(200);
    expect(res.body.items.every((t: any) => !t.isTransfer)).toBe(true);
    expect(res.body.items.some((t: any) => t.id === txnId)).toBe(true);
  });

  it("TXN-GET-ALL-014: without exclude flag includes transfer transactions", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const walletB = await createWallet(user, { initialBalance: 0, icon: "💳" });
    await createTransfer(user, walletA, walletB, { amount: 40, date: "2025-04-01" });
    const res = await api(user, "GET", "/transactions?limit=50");
    expect(res.status).toBe(200);
    expect(res.body.items.some((t: any) => t.isTransfer)).toBe(true);
  });
});
