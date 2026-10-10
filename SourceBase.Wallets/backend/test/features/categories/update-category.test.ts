import { describe, expect, it } from "vitest";
import { NIL_ID, api, createCategory, uniqueUser, unknownId } from "../../helpers";

const findCategory = async (user: string, id: string) => (await api(user, "GET", "/categories")).body.find((x: any) => x.id === id);

describe("UpdateCategory", () => {
  it("CATS-UPDATE-001: missing token returns 401", async () => {
    const res = await api(null, "PATCH", `/categories/${unknownId()}`, { name: `Updated_${crypto.randomUUID()}`, icon: "✏️" });
    expect(res.status).toBe(401);
  });

  it("CATS-UPDATE-002: valid data returns 200", async () => {
    const user = uniqueUser();
    const categoryId = await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const updatedName = `Updated_${crypto.randomUUID()}`;
    const res = await api(user, "PATCH", `/categories/${categoryId}`, { name: updatedName, icon: "🏷️" });
    expect(res.status).toBe(200);
    expect(res.body.id).toBe(categoryId);
    const updated = await findCategory(user, categoryId);
    expect(updated.name).toBe(updatedName);
    expect(updated.icon).toBe("🏷️");
  });

  it("CATS-UPDATE-003: only icon returns 200 and keeps name", async () => {
    const user = uniqueUser();
    const originalName = `Category_${crypto.randomUUID()}`;
    const categoryId = await createCategory(user, { name: originalName, type: "Expense", icon: "🏷️" });
    const res = await api(user, "PATCH", `/categories/${categoryId}`, { icon: "✏️" });
    expect(res.status).toBe(200);
    const updated = await findCategory(user, categoryId);
    expect(updated.name).toBe(originalName);
    expect(updated.icon).toBe("✏️");
  });

  it("CATS-UPDATE-004: system category returns 403", async () => {
    const user = uniqueUser();
    const list = await api(user, "GET", "/categories?type=Expense");
    expect(list.status).toBe(200);
    const systemCategory = list.body.find((x: any) => x.isSystem);
    const res = await api(user, "PATCH", `/categories/${systemCategory.id}`, { name: `System_${crypto.randomUUID()}`, icon: "🚫" });
    expect(res.status).toBe(403);
  });

  it("CATS-UPDATE-005: other user's category returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const categoryId = await createCategory(owner, { name: `Category_${crypto.randomUUID()}`, type: "Income", icon: "🏷️" });
    const res = await api(other, "PATCH", `/categories/${categoryId}`, { name: `OtherUser_${crypto.randomUUID()}`, icon: "🚫" });
    expect(res.status).toBe(404);
  });

  it("CATS-UPDATE-006: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "PATCH", `/categories/${unknownId()}`, { name: `Unknown_${crypto.randomUUID()}`, icon: "🏷️" });
    expect(res.status).toBe(404);
  });

  it("CATS-UPDATE-007: empty id returns 400", async () => {
    const res = await api(uniqueUser(), "PATCH", `/categories/${NIL_ID}`, { name: "Test" });
    expect(res.status).toBe(400);
  });
});
