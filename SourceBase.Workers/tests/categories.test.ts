import { beforeEach, describe, expect, it } from "vitest";
import { createCategory, createTestContext, createTransaction, createWallet, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers category routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("creates, lists, updates, and deletes user categories while protecting system categories", async () => {
    const initialListResponse = await send(context, "http://localhost/api/categories?type=Expense", {
      headers: { Authorization: `Bearer ${token}` }
    });

    expect(initialListResponse.status).toBe(200);
    const initialCategories = await initialListResponse.json() as Array<{
      id: string;
      userId: string | null;
      name: string;
      type: string;
      icon: string | null;
      isSystem: boolean;
    }>;
    const systemCategory = initialCategories.find(category => category.name === "Food & Drink");
    expect(systemCategory?.isSystem).toBe(true);
    expect(systemCategory?.userId ?? null).toBeNull();

    const createResult = await createCategory(context, token, { name: "Gym", type: "Expense", icon: "🏋️" });
    expect(createResult.response.status).toBe(200);

    const updateSystemResponse = await send(context, `http://localhost/api/categories/${systemCategory?.id}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ name: "Blocked" })
    });
    expect(updateSystemResponse.status).toBe(403);

    const patchResponse = await send(context, `http://localhost/api/categories/${createResult.body.id}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ name: "Fitness", icon: null })
    });
    expect(patchResponse.status).toBe(200);

    const listAfterUpdateResponse = await send(context, "http://localhost/api/categories?type=Expense", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const listAfterUpdate = await listAfterUpdateResponse.json() as Array<{
      id: string;
      name: string;
      icon: string | null;
      isSystem: boolean;
    }>;
    expect(listAfterUpdate.find(category => category.id === createResult.body.id)).toMatchObject({
      name: "Fitness",
      icon: "🏋️",
      isSystem: false
    });

    const deleteSystemResponse = await send(context, `http://localhost/api/categories/${systemCategory?.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteSystemResponse.status).toBe(403);

    const deleteResponse = await send(context, `http://localhost/api/categories/${createResult.body.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });

    const finalListResponse = await send(context, "http://localhost/api/categories?type=Expense", {
      headers: { Authorization: `Bearer ${token}` }
    });
    const finalList = await finalListResponse.json() as Array<{ id: string }>;
    expect(finalList.some(category => category.id === createResult.body.id)).toBe(false);
  });

  it("rejects deleting a category that is already used by a transaction", async () => {
    const wallet = await createWallet(context, token, { name: "Cash", initialBalance: 0, currency: "USD" });
    const category = await createCategory(context, token, { name: "Coffee", type: "Expense", icon: "☕" });
    await createTransaction(context, token, {
      walletId: wallet.body.id,
      amount: 5,
      type: "Expense",
      date: "2026-09-29",
      note: "Morning coffee",
      categoryId: category.body.id
    });

    const deleteResponse = await send(context, `http://localhost/api/categories/${category.body.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });

    expect(deleteResponse.status).toBe(400);
    expect(await deleteResponse.json()).toMatchObject({ message: "Category is in use by transactions" });
  });
});
