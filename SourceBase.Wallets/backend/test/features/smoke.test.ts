import { describe, expect, it } from "vitest";
import { api, createCategory, createTransaction, createWallet, uniqueUser } from "../helpers";

describe("smoke", () => {
  it("rejects anonymous requests", async () => {
    expect((await api(null, "GET", "/wallets")).status).toBe(401);
  });

  it("creates a wallet and computes its balance", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100 });
    const categoryId = await createCategory(user);
    await createTransaction(user, walletId, categoryId, { amount: 30, type: "Expense" });
    const res = await api(user, "GET", `/wallets/${walletId}`);
    expect(res.status).toBe(200);
    expect(res.body.balance).toBe(70);
  });
});
