import { describe, expect, it } from "vitest";
import { api, createIcon, uniqueUser } from "../../helpers";

describe("GetIcons", () => {
  it("ICONS-GET-001: without token return 401", async () => {
    const res = await api(null, "GET", "/icons");
    expect(res.status).toBe(401);
  });

  it("ICONS-GET-002: with token return 200 and all icons", async () => {
    const res = await api(uniqueUser(), "GET", "/icons");
    expect(res.status).toBe(200);
    expect(res.body.length).toBeGreaterThan(0);
  });

  it("ICONS-GET-003: with group filter return group and general icons", async () => {
    const res = await api(uniqueUser(), "GET", "/icons?group=Wallet");

    expect(res.status).toBe(200);
    expect(res.body.length).toBeGreaterThan(0);
    expect(res.body.every((i: any) => i.group === "Wallet" || i.group === "General")).toBe(true);
    expect(res.body.some((i: any) => i.group === "Wallet")).toBe(true);
    expect(res.body.some((i: any) => i.group === "General")).toBe(true);
  });

  it("ICONS-GET-004: with invalid group return all icons", async () => {
    const user = uniqueUser();
    const all = await api(user, "GET", "/icons");

    const res = await api(user, "GET", "/icons?group=NotAGroup");

    expect(res.status).toBe(200);
    expect(res.body.length).toBe(all.body.length);
  });

  it("ICONS-GET-005: results ordered by sort order", async () => {
    const res = await api(uniqueUser(), "GET", "/icons?group=Wallet");

    expect(res.status).toBe(200);
    const sortOrders = res.body.filter((i: any) => i.group === "Wallet").map((i: any) => i.sortOrder);
    expect(sortOrders).toEqual([...sortOrders].sort((a: number, b: number) => a - b));
  });

  it("ICONS-GET-006 (extra): another user's icon is not listed", async () => {
    const owner = uniqueUser("owner");
    const id = await createIcon(owner);

    const res = await api(uniqueUser("other"), "GET", "/icons");

    expect(res.body.some((i: any) => i.id === id)).toBe(false);
  });

  it("ICONS-GET-007 (extra): lists system icons plus the user's own icons", async () => {
    const user = uniqueUser();
    const id = await createIcon(user);

    const res = await api(user, "GET", "/icons");

    expect(res.body.some((i: any) => i.id === id && !i.isSystem)).toBe(true);
    expect(res.body.some((i: any) => i.isSystem)).toBe(true);
  });
});
