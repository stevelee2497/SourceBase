import { beforeEach, describe, expect, it } from "vitest";
import { createTestContext, createWallet, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers transfer routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("creates paired transfer transactions and deletes them together", async () => {
    const fromWallet = await createWallet(context, token, { name: "Primary", initialBalance: 100, currency: "USD", icon: "💳" });
    const toWallet = await createWallet(context, token, { name: "Savings", initialBalance: 10, currency: "USD", icon: "🏦" });

    const createResponse = await send(context, "http://localhost/api/transfers", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        fromWalletId: fromWallet.body.id,
        toWalletId: toWallet.body.id,
        amount: 40,
        date: "2026-09-29",
        note: "Emergency fund"
      })
    });
    expect(createResponse.status).toBe(200);
    const createBody = await createResponse.json() as { id: string };

    const transfersResponse = await send(context, "http://localhost/api/transfers", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(transfersResponse.status).toBe(200);
    const transfersBody = await transfersResponse.json() as {
      items: Array<{ id: string; fromWalletId: string; fromWalletName: string; toWalletId: string; toWalletName: string; amount: number; date: string; note: string | null }>;
      total: number;
    };
    expect(transfersBody.total).toBe(1);
    expect(transfersBody.items[0]).toMatchObject({
      id: createBody.id,
      fromWalletId: fromWallet.body.id,
      fromWalletName: "Primary",
      toWalletId: toWallet.body.id,
      toWalletName: "Savings",
      amount: 40,
      date: "2026-09-29",
      note: "Emergency fund"
    });

    const transactionsResponse = await send(context, "http://localhost/api/transactions", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const transactionsBody = await transactionsResponse.json() as {
      items: Array<{ id: string; amount: number; type: string; walletId: string; isTransfer: boolean }>;
      total: number;
    };
    expect(transactionsBody.total).toBe(2);
    expect(transactionsBody.items).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ walletId: fromWallet.body.id, amount: 40, type: "Expense", isTransfer: true }),
        expect.objectContaining({ walletId: toWallet.body.id, amount: 40, type: "Income", isTransfer: true })
      ])
    );

    const deleteTransferTransactionResponse = await send(context, `http://localhost/api/transactions/${transactionsBody.items[0]?.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteTransferTransactionResponse.status).toBe(400);
    expect(await deleteTransferTransactionResponse.json()).toMatchObject({
      message: "Transfer transactions cannot be deleted directly. Delete the transfer instead."
    });

    const walletsResponse = await send(context, "http://localhost/api/wallets", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const walletsBody = await walletsResponse.json() as {
      wallets: Array<{ id: string; balance: number }>;
      totalBalance: number;
    };
    expect(walletsBody.wallets.find(wallet => wallet.id === fromWallet.body.id)?.balance).toBe(60);
    expect(walletsBody.wallets.find(wallet => wallet.id === toWallet.body.id)?.balance).toBe(50);
    expect(walletsBody.totalBalance).toBe(110);

    const deleteTransferResponse = await send(context, `http://localhost/api/transfers/${createBody.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteTransferResponse.status).toBe(200);
    expect(await deleteTransferResponse.json()).toEqual({ success: true });

    const transfersAfterDeleteResponse = await send(context, "http://localhost/api/transfers", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const transfersAfterDelete = await transfersAfterDeleteResponse.json() as { total: number; items: unknown[] };
    expect(transfersAfterDelete.total).toBe(0);
    expect(transfersAfterDelete.items).toHaveLength(0);

    const transactionsAfterDeleteResponse = await send(context, "http://localhost/api/transactions", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const transactionsAfterDelete = await transactionsAfterDeleteResponse.json() as { total: number; items: unknown[] };
    expect(transactionsAfterDelete.total).toBe(0);
    expect(transactionsAfterDelete.items).toHaveLength(0);
  });

  it("validates that source and destination wallets differ", async () => {
    const wallet = await createWallet(context, token, { name: "Single", initialBalance: 10, currency: "USD", icon: "💵" });

    const response = await send(context, "http://localhost/api/transfers", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        fromWalletId: wallet.body.id,
        toWalletId: wallet.body.id,
        amount: 5,
        date: "2026-09-29",
        note: "Invalid"
      })
    });

    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({
      errors: {
        toWalletId: ["Source and destination wallets must be different."]
      }
    });
  });
});
