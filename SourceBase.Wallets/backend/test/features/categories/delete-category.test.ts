import { describe, expect, it } from "vitest";
import { api, createCategory, createWallet, uniqueUser, unknownId } from "../../helpers";

describe("DeleteCategory", () => {
  it("CATS-DELETE-001: no token returns 401", async () => {
    const res = await api(null, "DELETE", `/categories/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("CATS-DELETE-002: owned unused category returns 200", async () => {
    const user = uniqueUser();
    const categoryId = await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(user, "DELETE", `/categories/${categoryId}`);
    expect(res.status).toBe(200);
    expect(res.body.success).toBe(true);
    const list = await api(user, "GET", "/categories");
    expect(list.body.some((x: any) => x.id === categoryId)).toBe(false);
  });

  it("CATS-DELETE-003: system category returns 403", async () => {
    const user = uniqueUser();
    const list = await api(user, "GET", "/categories?type=Expense");
    expect(list.status).toBe(200);
    const systemCategory = list.body.find((x: any) => x.isSystem);
    const res = await api(user, "DELETE", `/categories/${systemCategory.id}`);
    expect(res.status).toBe(403);
  });

  it("CATS-DELETE-004: other user's category returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const categoryId = await createCategory(owner, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(other, "DELETE", `/categories/${categoryId}`);
    expect(res.status).toBe(404);
  });

  it("CATS-DELETE-005: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "DELETE", `/categories/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("CATS-DELETE-006: category with referenced transactions returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100 });
    const categoryId = await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const txn = await api(user, "POST", "/transactions", { walletId, amount: 20, type: "Expense", date: "2025-03-01", note: `Txn_${crypto.randomUUID()}`, categoryId });
    expect(txn.status).toBe(200);
    const res = await api(user, "DELETE", `/categories/${categoryId}`);
    expect(res.status).toBe(400);
    expect(JSON.stringify(res.body)).toContain("Category is in use by transactions");
  });
});
