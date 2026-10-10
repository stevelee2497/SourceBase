import { describe, expect, it } from "vitest";
import { api, createCategory, uniqueUser } from "../../helpers";

describe("GetCategories", () => {
  it("CATS-GET-001: without token returns 401", async () => {
    const res = await api(null, "GET", "/categories");
    expect(res.status).toBe(401);
  });

  it("CATS-GET-002: returns system and user categories", async () => {
    const user = uniqueUser();
    const customId = await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(user, "GET", "/categories");
    expect(res.status).toBe(200);
    expect(res.body.some((x: any) => x.id === customId && !x.isSystem)).toBe(true);
    expect(res.body.some((x: any) => x.isSystem)).toBe(true);
  });

  it("CATS-GET-003: multiple users exclude others' categories", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const ownId = await createCategory(owner, { name: `Own_${crypto.randomUUID()}`, type: "Income", icon: "🏷️" });
    const otherId = await createCategory(other, { name: `Other_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(owner, "GET", "/categories");
    expect(res.status).toBe(200);
    expect(res.body.some((x: any) => x.id === ownId)).toBe(true);
    expect(res.body.some((x: any) => x.id === otherId)).toBe(false);
  });

  it("CATS-GET-004: income filter returns only income categories", async () => {
    const user = uniqueUser();
    await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Income", icon: "🏷️" });
    await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(user, "GET", "/categories?type=Income");
    expect(res.status).toBe(200);
    expect(res.body.length).toBeGreaterThan(0);
    expect(res.body.every((x: any) => x.type === "Income")).toBe(true);
  });

  it("CATS-GET-005: expense filter returns only expense categories", async () => {
    const user = uniqueUser();
    await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Income", icon: "🏷️" });
    await createCategory(user, { name: `Category_${crypto.randomUUID()}`, type: "Expense", icon: "🏷️" });
    const res = await api(user, "GET", "/categories?type=Expense");
    expect(res.status).toBe(200);
    expect(res.body.length).toBeGreaterThan(0);
    expect(res.body.every((x: any) => x.type === "Expense")).toBe(true);
  });
});
