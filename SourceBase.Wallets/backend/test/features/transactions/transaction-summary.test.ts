import { describe, expect, it } from "vitest";
import { api, createCategory, createTransaction, createTransfer, createWallet, systemCategoryId, uniqueUser } from "../../helpers";

const note = () => `Txn_${crypto.randomUUID().replaceAll("-", "")}`;

describe("TransactionSummary", () => {
  it("TXN-SUMMARY-001: missing token returns 401", async () => {
    const res = await api(null, "GET", "/transactions/summary");
    expect(res.status).toBe(401);
  });

  it("TXN-SUMMARY-002: period returns total income and expense", async () => {
    const user = uniqueUser("transaction_summary");
    const walletId = await createWallet(user, { icon: "💳" });
    await createTransaction(user, walletId, await systemCategoryId(user, "Income"), { amount: 100, type: "Income", date: "2025-03-01", note: note() });
    await createTransaction(user, walletId, await systemCategoryId(user, "Expense"), { amount: 40, type: "Expense", date: "2025-03-02", note: note() });
    const res = await api(user, "GET", "/transactions/summary?dateFrom=2025-03-01&dateTo=2025-03-31");
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(100);
    expect(res.body.totalExpense).toBe(40);
  });

  it("TXN-SUMMARY-003: calculates net balance (income minus expense)", async () => {
    const user = uniqueUser("transaction_summary");
    const walletId = await createWallet(user, { icon: "💳" });
    await createTransaction(user, walletId, await systemCategoryId(user, "Income"), { amount: 100, type: "Income", date: "2025-03-03", note: note() });
    await createTransaction(user, walletId, await systemCategoryId(user, "Expense"), { amount: 40, type: "Expense", date: "2025-03-04", note: note() });
    const res = await api(user, "GET", "/transactions/summary?dateFrom=2025-03-01&dateTo=2025-03-31");
    expect(res.status).toBe(200);
    expect(res.body.netBalance).toBe(60);
  });

  it("TXN-SUMMARY-004: wallet filter returns only specified wallet totals", async () => {
    const user = uniqueUser("transaction_summary");
    const firstWallet = await createWallet(user, { icon: "💳" });
    const secondWallet = await createWallet(user, { icon: "💳" });
    await createTransaction(user, firstWallet, await systemCategoryId(user, "Income"), { amount: 100, type: "Income", date: "2025-03-05", note: note() });
    await createTransaction(user, secondWallet, await systemCategoryId(user, "Expense"), { amount: 40, type: "Expense", date: "2025-03-05", note: note() });
    const res = await api(user, "GET", `/transactions/summary?walletIds=${firstWallet}`);
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(100);
    expect(res.body.totalExpense).toBe(0);
    expect(res.body.netBalance).toBe(100);
  });

  it("TXN-SUMMARY-005: date range returns transactions within the specified range", async () => {
    const user = uniqueUser("transaction_summary");
    const walletId = await createWallet(user, { icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    await createTransaction(user, walletId, incomeCategoryId, { amount: 10, type: "Income", date: "2025-02-28", note: note() });
    await createTransaction(user, walletId, incomeCategoryId, { amount: 30, type: "Income", date: "2025-03-10", note: note() });
    await createTransaction(user, walletId, expenseCategoryId, { amount: 20, type: "Expense", date: "2025-04-01", note: note() });
    const res = await api(user, "GET", "/transactions/summary?dateFrom=2025-03-01&dateTo=2025-03-31");
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(30);
    expect(res.body.totalExpense).toBe(0);
  });

  it("TXN-SUMMARY-006: by category returns grouped totals", async () => {
    const user = uniqueUser("transaction_summary");
    const walletId = await createWallet(user, { icon: "💳" });
    const salaryCategoryId = await systemCategoryId(user, "Income");
    const foodCategoryId = await createCategory(user, { name: `Food_${crypto.randomUUID().replaceAll("-", "")}`, type: "Expense", icon: "🍔" });
    await createTransaction(user, walletId, salaryCategoryId, { amount: 120, type: "Income", date: "2025-03-11", note: note() });
    await createTransaction(user, walletId, foodCategoryId, { amount: 35, type: "Expense", date: "2025-03-12", note: note() });
    await createTransaction(user, walletId, foodCategoryId, { amount: 15, type: "Expense", date: "2025-03-13", note: note() });
    const res = await api(user, "GET", "/transactions/summary?dateFrom=2025-03-01&dateTo=2025-03-31");
    expect(res.status).toBe(200);
    expect(res.body.byCategory.some((x: any) => x.categoryId === salaryCategoryId && x.total === 120 && x.type === "Income")).toBe(true);
    expect(res.body.byCategory.some((x: any) => x.categoryId === foodCategoryId && x.total === 50 && x.type === "Expense")).toBe(true);
  });

  it("TXN-SUMMARY-007: multiple users isolates each user's transactions", async () => {
    const owner = uniqueUser("transaction_summary");
    const other = uniqueUser("transaction_summary");
    const ownerWallet = await createWallet(owner, { icon: "💳" });
    const otherWallet = await createWallet(other, { icon: "💳" });
    await createTransaction(owner, ownerWallet, await systemCategoryId(owner, "Income"), { amount: 75, type: "Income", date: "2025-03-14", note: note() });
    await createTransaction(other, otherWallet, await systemCategoryId(other, "Income"), { amount: 200, type: "Income", date: "2025-03-14", note: note() });
    const res = await api(owner, "GET", "/transactions/summary?dateFrom=2025-03-01&dateTo=2025-03-31");
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(75);
    expect(res.body.totalExpense).toBe(0);
  });

  // Extra (not in .NET): the API now takes repeated `walletIds` (docs/api.md, difference #1).
  it("TXN-SUMMARY-008: multiple wallet ids filter sums only the specified wallets", async () => {
    const user = uniqueUser("transaction_summary");
    const walletA = await createWallet(user, { icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    const walletC = await createWallet(user, { icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    const expenseCategoryId = await systemCategoryId(user, "Expense");
    await createTransaction(user, walletA, incomeCategoryId, { amount: 100, type: "Income", date: "2025-03-05", note: note() });
    await createTransaction(user, walletB, expenseCategoryId, { amount: 40, type: "Expense", date: "2025-03-05", note: note() });
    await createTransaction(user, walletC, incomeCategoryId, { amount: 999, type: "Income", date: "2025-03-05", note: note() });
    const res = await api(user, "GET", `/transactions/summary?walletIds=${walletA}&walletIds=${walletB}`);
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(100);
    expect(res.body.totalExpense).toBe(40);
    expect(res.body.netBalance).toBe(60);
  });

  // Extra (not in .NET test suite): docs/api.md says the summary leaves transfers out.
  it("TXN-SUMMARY-009: transfers are excluded from totals", async () => {
    const user = uniqueUser("transaction_summary");
    const walletA = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const walletB = await createWallet(user, { icon: "💳" });
    await createTransaction(user, walletA, await systemCategoryId(user, "Income"), { amount: 10, type: "Income", date: "2025-03-05", note: note() });
    await createTransfer(user, walletA, walletB, { amount: 30, date: "2025-03-06" });
    const res = await api(user, "GET", "/transactions/summary");
    expect(res.status).toBe(200);
    expect(res.body.totalIncome).toBe(10);
    expect(res.body.totalExpense).toBe(0);
  });
});
