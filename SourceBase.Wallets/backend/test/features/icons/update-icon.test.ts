import { describe, expect, it } from "vitest";
import { NIL_ID, api, createIcon, unknownId, uniqueUser } from "../../helpers";

describe("UpdateIcon", () => {
  it("ICONS-UPDATE-001: without token returns 401", async () => {
    const res = await api(null, "PATCH", `/icons/${unknownId()}`, { value: "🦊", name: "Fox", group: "General", sortOrder: 1 });
    expect(res.status).toBe(401);
  });

  it("ICONS-UPDATE-002: valid request returns 200", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🦊", name: `Fox_${crypto.randomUUID()}`, group: "General", sortOrder: 1 });

    const res = await api(user, "PATCH", `/icons/${id}`, { value: "🐺", name: "Wolf", group: "General", sortOrder: 2 });

    expect(res.status).toBe(200);
    expect(res.body.id).toBe(id);
  });

  it("ICONS-UPDATE-003: persists changes", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🦊", name: `Fox_${crypto.randomUUID()}`, group: "Wallet", sortOrder: 50 });
    await api(user, "PATCH", `/icons/${id}`, { value: "🐺", name: "Wolf_Updated", group: "Category", sortOrder: 99 });

    const list = await api(user, "GET", "/icons");

    const updated = list.body.find((i: any) => i.id === id);
    expect(updated).toBeDefined();
    expect(updated.value).toBe("🐺");
    expect(updated.name).toBe("Wolf_Updated");
    expect(updated.group).toBe("Category");
    expect(updated.sortOrder).toBe(99);
  });

  it("ICONS-UPDATE-004: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "PATCH", `/icons/${unknownId()}`, { value: "🦊", name: "Fox", group: "General", sortOrder: 1 });
    expect(res.status).toBe(404);
  });

  it("ICONS-UPDATE-005: system icon returns 403", async () => {
    const user = uniqueUser();
    const list = await api(user, "GET", "/icons");
    const systemIcon = list.body.find((i: any) => i.isSystem);

    const res = await api(user, "PATCH", `/icons/${systemIcon.id}`, { value: "❌", name: "Hacked", group: "General", sortOrder: 0 });

    expect(res.status).toBe(403);
  });

  it("ICONS-UPDATE-006: empty value returns 400", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🦊", name: `Fox_${crypto.randomUUID()}`, group: "General", sortOrder: 1 });

    const res = await api(user, "PATCH", `/icons/${id}`, { value: "", name: "Fox", group: "General", sortOrder: 1 });

    expect(res.status).toBe(400);
  });

  it("ICONS-UPDATE-007: empty id returns 400", async () => {
    const res = await api(uniqueUser(), "PATCH", `/icons/${NIL_ID}`, { value: "🦊" });
    expect(res.status).toBe(400);
  });

  it("ICONS-UPDATE-008 (extra): another user's icon returns 404 and is left unchanged", async () => {
    const owner = uniqueUser("owner");
    const id = await createIcon(owner, { value: "🦊", name: "Fox_Original" });

    const res = await api(uniqueUser("other"), "PATCH", `/icons/${id}`, { name: "Hacked" });

    expect(res.status).toBe(404);
    const list = await api(owner, "GET", "/icons");
    expect(list.body.find((i: any) => i.id === id).name).toBe("Fox_Original");
  });

  it("ICONS-UPDATE-009 (extra): partial update keeps omitted fields", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🦊", name: "Fox_Keep", group: "Wallet", sortOrder: 7 });

    await api(user, "PATCH", `/icons/${id}`, { name: "Fox_Renamed" });

    const list = await api(user, "GET", "/icons");
    const updated = list.body.find((i: any) => i.id === id);
    expect(updated).toMatchObject({ value: "🦊", name: "Fox_Renamed", group: "Wallet", sortOrder: 7 });
  });
});
