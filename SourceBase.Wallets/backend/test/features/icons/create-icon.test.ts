import { describe, expect, it } from "vitest";
import { api, uniqueUser } from "../../helpers";

describe("CreateIcon", () => {
  it("ICONS-CREATE-001: missing token returns 401", async () => {
    const res = await api(null, "POST", "/icons", { value: "⭐", name: "Star", group: "General", sortOrder: 99 });
    expect(res.status).toBe(401);
  });

  it("ICONS-CREATE-002: valid request returns 200 and icon id", async () => {
    const res = await api(uniqueUser(), "POST", "/icons", { value: "🦊", name: `Fox_${crypto.randomUUID()}`, group: "General", sortOrder: 99 });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
  });

  it("ICONS-CREATE-003: empty value returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons", { value: "", name: "Test", group: "General", sortOrder: 1 });
    expect(res.status).toBe(400);
  });

  it("ICONS-CREATE-004: empty name returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons", { value: "🦊", name: "", group: "General", sortOrder: 1 });
    expect(res.status).toBe(400);
  });

  it("ICONS-CREATE-005: value exceeding max length returns 200", async () => {
    const res = await api(uniqueUser(), "POST", "/icons", { value: "x".repeat(2001), name: "Test", group: "General", sortOrder: 1 });
    expect(res.status).toBe(200);
  });

  it("ICONS-CREATE-006: name exceeding max length returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons", { value: "🦊", name: "x".repeat(101), group: "General", sortOrder: 1 });
    expect(res.status).toBe(400);
  });

  it("ICONS-CREATE-007: created icon appears in get icons", async () => {
    const user = uniqueUser();
    const uniqueName = `Unique_${crypto.randomUUID()}`;
    const created = await api(user, "POST", "/icons", { value: "🦋", name: uniqueName, group: "General", sortOrder: 999 });
    expect(created.status).toBe(200);

    const res = await api(user, "GET", "/icons");

    expect(res.body.some((i: any) => i.name === uniqueName)).toBe(true);
  });

  it("ICONS-CREATE-008 (extra): created icon is not a system icon", async () => {
    const user = uniqueUser();
    const created = await api(user, "POST", "/icons", { value: "🦊", name: `Fox_${crypto.randomUUID()}`, group: "General", sortOrder: 1 });

    const res = await api(user, "GET", "/icons");

    expect(res.body.find((i: any) => i.id === created.body.id).isSystem).toBe(false);
  });
});
