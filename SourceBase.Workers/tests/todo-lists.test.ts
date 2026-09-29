import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import type { AppBindings } from "../src/types";
import { authHeaders, createVerifiedUser, getUserInfo, send, setupTestBindings } from "./support/test-helpers";

describe("SourceBase.Workers todo list routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;

  beforeEach(async () => {
    bindings = await setupTestBindings();
    app = createApp();
  });

  it("requires auth for list retrieval", async () => {
    const response = await send(app, bindings, "http://localhost/api/todo-lists");
    expect(response.status).toBe(401);
  });

  it("creates and lists the current user's todo lists with isDefault and itemCount", async () => {
    const user = await createVerifiedUser(app, bindings);
    const userInfo = await getUserInfo(app, bindings, user.accessToken);

    const betaResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Beta" })
    });
    const betaBody = await betaResponse.json() as { id: string };

    const alphaResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Alpha" })
    });
    const alphaBody = await alphaResponse.json() as { id: string };

    await send(app, bindings, "http://localhost/api/auth/info", {
      method: "PATCH",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ defaultTodoListId: betaBody.id })
    });

    await bindings.DB.prepare(
      `INSERT INTO todo_items (id, title, date, status, user_id, todo_list_id, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(crypto.randomUUID(), "List Count Item", "2025-06-01", "Open", userInfo.id, betaBody.id, new Date().toISOString(), userInfo.userName)
      .run();

    const otherUser = await createVerifiedUser(app, bindings);
    await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(otherUser.accessToken),
      body: JSON.stringify({ name: "Should Not Appear" })
    });

    const response = await send(app, bindings, "http://localhost/api/todo-lists?orderBy=Name&order=Asc&page=1&limit=10", {
      headers: authHeaders(user.accessToken)
    });

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ id: string; name: string; itemCount: number; isDefault: boolean }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(body.page).toBe(1);
    expect(body.limit).toBe(10);
    expect(body.total).toBe(2);
    expect(body.items.map(item => item.name)).toEqual(["Alpha", "Beta"]);
    expect(body.items.find(item => item.id === alphaBody.id)?.isDefault).toBe(false);
    expect(body.items.find(item => item.id === betaBody.id)).toMatchObject({ itemCount: 1, isDefault: true });
  });

  it("keeps the existing name when patch payload uses null", async () => {
    const user = await createVerifiedUser(app, bindings);
    const createResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Original Name" })
    });
    const created = await createResponse.json() as { id: string };

    const patchResponse = await send(app, bindings, `http://localhost/api/todo-lists/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: null })
    });

    expect(patchResponse.status).toBe(200);

    const listResponse = await send(app, bindings, "http://localhost/api/todo-lists?limit=100", {
      headers: authHeaders(user.accessToken)
    });
    const listBody = await listResponse.json() as {
      items: Array<{ id: string; name: string }>;
    };

    expect(listBody.items.find(item => item.id === created.id)?.name).toBe("Original Name");
  });

  it("deletes only the current user's todo list", async () => {
    const owner = await createVerifiedUser(app, bindings);
    const stranger = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({ name: "Delete Me" })
    });
    const created = await createResponse.json() as { id: string };

    const forbiddenDelete = await send(app, bindings, `http://localhost/api/todo-lists/${created.id}`, {
      method: "DELETE",
      headers: authHeaders(stranger.accessToken)
    });
    expect(forbiddenDelete.status).toBe(404);

    const deleteResponse = await send(app, bindings, `http://localhost/api/todo-lists/${created.id}`, {
      method: "DELETE",
      headers: authHeaders(owner.accessToken)
    });

    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });

    const listResponse = await send(app, bindings, "http://localhost/api/todo-lists?limit=100", {
      headers: authHeaders(owner.accessToken)
    });
    const listBody = await listResponse.json() as { items: Array<{ id: string }> };
    expect(listBody.items.some(item => item.id === created.id)).toBe(false);
  });
});
