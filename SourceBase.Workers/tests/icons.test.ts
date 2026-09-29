import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import { authHeaders, getUserInfo, send, setupTestBindings } from "./support/test-helpers";
import type { AppBindings } from "../src/types";

describe("SourceBase.Workers icon routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;
  let accessToken: string;

  beforeEach(async () => {
    app = createApp();
    bindings = await setupTestBindings();

    const loginResponse = await send(app, bindings, "http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: bindings.ADMIN_EMAIL, password: bindings.ADMIN_PASSWORD })
    });
    const loginBody = await loginResponse.json() as { accessToken: string };
    accessToken = loginBody.accessToken;
    await getUserInfo(app, bindings, accessToken);
  });

  it("requires auth for get icons", async () => {
    const response = await send(app, bindings, "http://localhost/api/icons");
    expect(response.status).toBe(401);
  });

  it("returns seeded icons and includes General icons in group filters", async () => {
    const allResponse = await send(app, bindings, "http://localhost/api/icons", {
      headers: authHeaders(accessToken)
    });
    const walletResponse = await send(app, bindings, "http://localhost/api/icons?group=Wallet", {
      headers: authHeaders(accessToken)
    });

    expect(allResponse.status).toBe(200);
    expect(walletResponse.status).toBe(200);

    const allIcons = await allResponse.json() as Array<{ group: string; isSystem: boolean }>;
    const walletIcons = await walletResponse.json() as Array<{ group: string; sortOrder: number }>;

    expect(allIcons.some(icon => icon.isSystem && icon.group === "General")).toBe(true);
    expect(walletIcons.some(icon => icon.group === "Wallet")).toBe(true);
    expect(walletIcons.some(icon => icon.group === "General")).toBe(true);
    expect(walletIcons.every(icon => icon.group === "Wallet" || icon.group === "General")).toBe(true);

    const walletSortOrders = walletIcons.filter(icon => icon.group === "Wallet").map(icon => icon.sortOrder);
    expect(walletSortOrders).toEqual([...walletSortOrders].sort((left, right) => left - right));
  });

  it("creates, updates, and deletes custom icons", async () => {
    const createResponse = await send(app, bindings, "http://localhost/api/icons", {
      method: "POST",
      headers: authHeaders(accessToken),
      body: JSON.stringify({ value: "🦊", name: "Fox", group: "General", sortOrder: 99 })
    });
    expect(createResponse.status).toBe(200);
    const created = await createResponse.json() as { id: string };

    const updateResponse = await send(app, bindings, `http://localhost/api/icons/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(accessToken),
      body: JSON.stringify({ value: "🐺", name: "Wolf", group: "Wallet", sortOrder: 3 })
    });
    expect(updateResponse.status).toBe(200);
    expect(await updateResponse.json()).toEqual({ id: created.id });

    const listResponse = await send(app, bindings, "http://localhost/api/icons", {
      headers: authHeaders(accessToken)
    });
    const icons = await listResponse.json() as Array<{ id: string; value: string; name: string; group: string; sortOrder: number }>;
    expect(icons).toContainEqual(expect.objectContaining({
      id: created.id,
      value: "🐺",
      name: "Wolf",
      group: "Wallet",
      sortOrder: 3
    }));

    const deleteResponse = await send(app, bindings, `http://localhost/api/icons/${created.id}`, {
      method: "DELETE",
      headers: authHeaders(accessToken)
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });
  });

  it("rejects writes to system icons", async () => {
    const listResponse = await send(app, bindings, "http://localhost/api/icons", {
      headers: authHeaders(accessToken)
    });
    const icons = await listResponse.json() as Array<{ id: string; isSystem: boolean }>;
    const systemIcon = icons.find(icon => icon.isSystem);
    expect(systemIcon).toBeTruthy();

    const updateResponse = await send(app, bindings, `http://localhost/api/icons/${systemIcon?.id}`, {
      method: "PATCH",
      headers: authHeaders(accessToken),
      body: JSON.stringify({ name: "Blocked" })
    });
    expect(updateResponse.status).toBe(403);

    const deleteResponse = await send(app, bindings, `http://localhost/api/icons/${systemIcon?.id}`, {
      method: "DELETE",
      headers: authHeaders(accessToken)
    });
    expect(deleteResponse.status).toBe(403);
  });

  it("returns upload contract with mapped content type and validates extensions", async () => {
    const uploadResponse = await send(app, bindings, "http://localhost/api/icons/upload-image", {
      method: "POST",
      headers: authHeaders(accessToken),
      body: JSON.stringify({ fileName: "icon.svg" })
    });
    expect(uploadResponse.status).toBe(200);
    const upload = await uploadResponse.json() as { uploadUrl: string; iconUrl: string; contentType: string };
    expect(upload.contentType).toBe("image/svg+xml");
    expect(upload.uploadUrl).toContain("/uploads/icons/");
    expect(upload.iconUrl).toContain("/icons/");

    const invalidResponse = await send(app, bindings, "http://localhost/api/icons/upload-image", {
      method: "POST",
      headers: authHeaders(accessToken),
      body: JSON.stringify({ fileName: "icon.bmp" })
    });
    expect(invalidResponse.status).toBe(400);
  });
});
