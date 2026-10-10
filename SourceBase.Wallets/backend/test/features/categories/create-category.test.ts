import { describe, expect, it } from "vitest";
import { api, uniqueUser } from "../../helpers";

describe("CreateCategory", () => {
  it("CATS-CREATE-001: missing token returns 401", async () => {
    const res = await api(null, "POST", "/categories", { name: `Unauthorized_${crypto.randomUUID()}`, type: "Expense" });
    expect(res.status).toBe(401);
  });

  it("CATS-CREATE-002: valid data returns 200 and id", async () => {
    const res = await api(uniqueUser(), "POST", "/categories", { name: `Created_${crypto.randomUUID()}`, type: "Expense", icon: "🧾" });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
  });

  it("CATS-CREATE-003: missing name returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/categories", { type: "Expense" });
    expect(res.status).toBe(400);
  });

  it("CATS-CREATE-004: missing type returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/categories", { name: `NoType_${crypto.randomUUID()}` });
    expect(res.status).toBe(400);
  });

  it("CATS-CREATE-005: invalid type returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/categories", { name: `InvalidType_${crypto.randomUUID()}`, type: "InvalidType" });
    expect(res.status).toBe(400);
  });

  it("CATS-CREATE-006: authenticated user creates non-system user-owned category", async () => {
    const user = uniqueUser("category_owner");
    const created = await api(user, "POST", "/categories", { name: `Category_${crypto.randomUUID()}`, type: "Income", icon: "🏷️" });
    expect(created.status).toBe(200);
    const list = await api(user, "GET", "/categories");
    const matches = list.body.filter((x: any) => x.id === created.body.id);
    expect(matches).toHaveLength(1);
    expect(matches[0].isSystem).toBe(false);
    expect(matches[0].userId).toBe(user);
  });
});
