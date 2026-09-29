import { beforeEach, describe, expect, it } from "vitest";
import { createCategory, createTestContext, createTransaction, createWallet, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers transaction routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("creates, lists, gets, updates, summarizes, and deletes transactions", async () => {
    const wallet = await createWallet(context, token, { name: "Checking", initialBalance: 100, currency: "USD", icon: "🏦" });
    const category = await createCategory(context, token, { name: "Groceries", type: "Expense", icon: "🛒" });

    const createResult = await createTransaction(context, token, {
      walletId: wallet.body.id,
      amount: 30,
      type: "Expense",
      date: "2026-09-28",
      note: "Weekly grocery run",
      categoryId: category.body.id
    });
    expect(createResult.response.status).toBe(200);

    const listResponse = await send(context, "http://localhost/api/transactions", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(listResponse.status).toBe(200);
    const listBody = await listResponse.json() as {
      items: Array<{ id: string; amount: number; type: string; date: string; note: string | null; walletId: string; walletName: string; categoryId: string | null; categoryName: string | null; isTransfer: boolean }>;
      page: number;
      limit: number;
      total: number;
    };
    expect(listBody.total).toBe(1);
    expect(listBody.items[0]).toMatchObject({
      id: createResult.body.id,
      amount: 30,
      type: "Expense",
      date: "2026-09-28",
      note: "Weekly grocery run",
      walletId: wallet.body.id,
      walletName: "Checking",
      categoryId: category.body.id,
      categoryName: "Groceries",
      isTransfer: false
    });

    const getResponse = await send(context, `http://localhost/api/transactions/${createResult.body.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(getResponse.status).toBe(200);

    const patchResponse = await send(context, `http://localhost/api/transactions/${createResult.body.id}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ amount: 45.5, note: null })
    });
    expect(patchResponse.status).toBe(200);

    const updatedGetResponse = await send(context, `http://localhost/api/transactions/${createResult.body.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const updatedBody = await updatedGetResponse.json() as { amount: number; note: string | null };
    expect(updatedBody).toMatchObject({ amount: 45.5, note: "Weekly grocery run" });

    const transferWallet = await createWallet(context, token, { name: "Savings", initialBalance: 0, currency: "USD", icon: "💰" });
    const transferResponse = await send(context, "http://localhost/api/transfers", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        fromWalletId: wallet.body.id,
        toWalletId: transferWallet.body.id,
        amount: 20,
        date: "2026-09-29",
        note: "Move to savings"
      })
    });
    expect(transferResponse.status).toBe(200);

    const filteredListResponse = await send(context, "http://localhost/api/transactions?exclude=Transfer", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const filteredListBody = await filteredListResponse.json() as { total: number; items: Array<{ id: string }> };
    expect(filteredListBody.total).toBe(1);
    expect(filteredListBody.items).toHaveLength(1);
    expect(filteredListBody.items[0]?.id).toBe(createResult.body.id);

    const summaryResponse = await send(context, "http://localhost/api/transactions/summary", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(summaryResponse.status).toBe(200);
    const summaryBody = await summaryResponse.json() as {
      totalIncome: number;
      totalExpense: number;
      netBalance: number;
      byCategory: Array<{ categoryId: string | null; categoryName: string | null; type: string; total: number }>;
    };
    expect(summaryBody.totalIncome).toBe(0);
    expect(summaryBody.totalExpense).toBe(45.5);
    expect(summaryBody.netBalance).toBe(-45.5);
    expect(summaryBody.byCategory).toContainEqual({
      categoryId: category.body.id,
      categoryName: "Groceries",
      type: "Expense",
      total: 45.5
    });

    const deleteResponse = await send(context, `http://localhost/api/transactions/${createResult.body.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });
  });

  it("returns field validation errors when create transaction wallet or category is missing", async () => {
    const response = await send(context, "http://localhost/api/transactions", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        walletId: crypto.randomUUID(),
        amount: 10,
        type: "Income",
        date: "2026-09-29",
        note: "Ghost",
        categoryId: crypto.randomUUID()
      })
    });

    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({
      message: "One or more validation errors occurred.",
      errors: {
        walletId: ["Wallet not found."],
        categoryId: ["Category not found."]
      }
    });
  });
});
