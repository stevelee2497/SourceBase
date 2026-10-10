import { describe, expect, it } from "vitest";
import { api, createTransaction, createWallet, systemCategoryId, unknownId, uniqueUser } from "../../helpers";

describe("GetTransaction", () => {
  it("TXN-GET-001: missing token return 401", async () => {
    const res = await api(null, "GET", `/transactions/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("TXN-GET-002: owned transaction returns 200 and transaction data", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { icon: "💳" });
    const categories = await api<{ id: string; name: string; isSystem: boolean }[]>(user, "GET", "/categories?type=Income");
    const category = categories.body.find((x) => x.isSystem)!;
    const id = await createTransaction(user, walletId, category.id, { amount: 40, type: "Income", date: "2025-01-20", note: "Salary payment" });
    const res = await api(user, "GET", `/transactions/${id}`);
    expect(res.status).toBe(200);
    expect(res.body.id).toBe(id);
    expect(res.body.walletId).toBe(walletId);
    expect(res.body.walletName.startsWith("Wallet_")).toBe(true);
    expect(res.body.categoryId).toBe(category.id);
    expect(res.body.categoryName).toBe(category.name);
    expect(res.body.type).toBe("Income");
    expect(res.body.note).toBe("Salary payment");
    expect(res.body.isTransfer).toBe(false);
  });

  it("TXN-GET-003: unknown id return 404", async () => {
    const res = await api(uniqueUser(), "GET", `/transactions/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("TXN-GET-004: other user's transaction return 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const walletId = await createWallet(owner);
    const categoryId = await systemCategoryId(owner, "Income");
    const id = await createTransaction(owner, walletId, categoryId, { amount: 10, type: "Income", date: "2025-01-21", note: `Txn_${crypto.randomUUID()}` });
    const res = await api(other, "GET", `/transactions/${id}`);
    expect(res.status).toBe(404);
  });
});
