import { describe, expect, it } from "vitest";
import { api, createCategory, createWallet, getWallet, systemCategoryId, uniqueUser } from "../../helpers";

describe("CreateTransaction", () => {
  it("TXN-CREATE-001: missing token returns 401", async () => {
    const res = await api(null, "POST", "/transactions", { walletId: crypto.randomUUID(), amount: 100, type: "Income", date: "2025-01-01", categoryId: crypto.randomUUID() });
    expect(res.status).toBe(401);
  });

  it("TXN-CREATE-002: income transaction updates wallet balance correctly", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "POST", "/transactions", { walletId, amount: 25, type: "Income", date: "2025-01-01", note: `Income_${crypto.randomUUID()}`, categoryId });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
    expect((await getWallet(user, walletId)).balance).toBe(125);
  });

  it("TXN-CREATE-003: expense transaction updates wallet balance correctly", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100, icon: "💳" });
    const categoryId = await systemCategoryId(user, "Expense");
    const res = await api(user, "POST", "/transactions", { walletId, amount: 30, type: "Expense", date: "2025-01-02", note: `Expense_${crypto.randomUUID()}`, categoryId });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
    expect((await getWallet(user, walletId)).balance).toBe(70);
  });

  it("TXN-CREATE-004: missing wallet id returns 400", async () => {
    const user = uniqueUser();
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "POST", "/transactions", { amount: 20, type: "Income", date: "2025-01-03", categoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-005: missing amount returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user);
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "POST", "/transactions", { walletId, type: "Income", date: "2025-01-04", categoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-006: zero or negative amount returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user);
    const categoryId = await systemCategoryId(user, "Income");
    const zero = await api(user, "POST", "/transactions", { walletId, amount: 0, type: "Income", date: "2025-01-05", categoryId });
    const negative = await api(user, "POST", "/transactions", { walletId, amount: -1, type: "Expense", date: "2025-01-05", categoryId });
    expect(zero.status).toBe(400);
    expect(negative.status).toBe(400);
  });

  it("TXN-CREATE-007: missing date returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user);
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "POST", "/transactions", { walletId, amount: 20, type: "Income", categoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-008: missing type returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user);
    const categoryId = await systemCategoryId(user, "Income");
    const res = await api(user, "POST", "/transactions", { walletId, amount: 20, date: "2025-01-06", categoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-009: other user's wallet returns 400", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const otherWalletId = await createWallet(other);
    const categoryId = await systemCategoryId(owner, "Income");
    const res = await api(owner, "POST", "/transactions", { walletId: otherWalletId, amount: 20, type: "Income", date: "2025-01-07", categoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-010: other user's category returns 400", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const walletId = await createWallet(owner);
    const otherCategoryId = await createCategory(other, { type: "Expense", icon: "🏷️" });
    const res = await api(owner, "POST", "/transactions", { walletId, amount: 20, type: "Expense", date: "2025-01-08", categoryId: otherCategoryId });
    expect(res.status).toBe(400);
  });

  it("TXN-CREATE-011: missing category returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user);
    const res = await api(user, "POST", "/transactions", { walletId, amount: 45, type: "Income", date: "2025-01-09", note: `NoCategory_${crypto.randomUUID()}` });
    expect(res.status).toBe(400);
  });
});
