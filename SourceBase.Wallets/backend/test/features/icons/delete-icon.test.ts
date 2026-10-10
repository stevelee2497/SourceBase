import { describe, expect, it } from "vitest";
import { api, createIcon, unknownId, uniqueUser } from "../../helpers";

describe("DeleteIcon", () => {
  it("ICONS-DELETE-001: without token returns 401", async () => {
    const res = await api(null, "DELETE", `/icons/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("ICONS-DELETE-002: valid id returns 200", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🗑️", name: `ToDelete_${crypto.randomUUID()}` });

    const res = await api(user, "DELETE", `/icons/${id}`);

    expect(res.status).toBe(200);
    expect(res.body.success).toBe(true);
  });

  it("ICONS-DELETE-003: deleted icon removed from list", async () => {
    const user = uniqueUser();
    const id = await createIcon(user, { value: "🗑️", name: `Delete_${crypto.randomUUID()}` });

    await api(user, "DELETE", `/icons/${id}`);

    const list = await api(user, "GET", "/icons");
    expect(list.body.some((i: any) => i.id === id)).toBe(false);
  });

  it("ICONS-DELETE-004: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "DELETE", `/icons/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("ICONS-DELETE-005: system icon returns 403", async () => {
    const user = uniqueUser();
    const list = await api(user, "GET", "/icons");
    const systemIcon = list.body.find((i: any) => i.isSystem);

    const res = await api(user, "DELETE", `/icons/${systemIcon.id}`);

    expect(res.status).toBe(403);
  });

  it("ICONS-DELETE-006 (extra): another user's icon returns 404 and is left intact", async () => {
    const owner = uniqueUser("owner");
    const id = await createIcon(owner);

    const res = await api(uniqueUser("other"), "DELETE", `/icons/${id}`);

    expect(res.status).toBe(404);
    const list = await api(owner, "GET", "/icons");
    expect(list.body.some((i: any) => i.id === id)).toBe(true);
  });
});
