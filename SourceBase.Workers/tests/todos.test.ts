import { beforeEach, describe, expect, it } from "vitest";
import { createApp } from "../src/app";
import type { AppBindings } from "../src/types";
import { authHeaders, createVerifiedUser, send, setupTestBindings } from "./support/test-helpers";

describe("SourceBase.Workers todo routes", () => {
  let app: ReturnType<typeof createApp>;
  let bindings: AppBindings;

  beforeEach(async () => {
    bindings = await setupTestBindings();
    app = createApp();
  });

  it("creates a todo with a valid todo list and returns the stored item", async () => {
    const user = await createVerifiedUser(app, bindings);

    const listResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Project List" })
    });
    const list = await listResponse.json() as { id: string };

    const createResponse = await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        date: "2025-06-01",
        title: "Integration Todo",
        status: "Open",
        todoListId: list.id
      })
    });

    expect(createResponse.status).toBe(200);
    const created = await createResponse.json() as { id: string };

    const getResponse = await send(app, bindings, `http://localhost/api/todos/${created.id}`, {
      headers: authHeaders(user.accessToken)
    });

    expect(getResponse.status).toBe(200);
    expect(await getResponse.json()).toMatchObject({
      id: created.id,
      date: "2025-06-01",
      title: "Integration Todo",
      status: "Open",
      todoListId: list.id
    });

    const notifications = await bindings.DB.prepare(
      `SELECT title, event, is_read
       FROM notifications
       WHERE user_id = ?
       ORDER BY created_on DESC`
    )
      .bind((await bindings.DB.prepare(`SELECT user_id FROM todo_lists WHERE id = ?`).bind(list.id).first<string>("user_id"))!)
      .all<{ title: string; event: string; is_read: number }>();

    expect(notifications.results[0]).toMatchObject({
      title: "Todo item created",
      event: "TodoCreatedEvent",
      is_read: 1
    });
  });

  it("rejects a todo create request when the todo list does not belong to the current user", async () => {
    const owner = await createVerifiedUser(app, bindings);
    const stranger = await createVerifiedUser(app, bindings);

    const listResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(owner.accessToken),
      body: JSON.stringify({ name: "Private List" })
    });
    const list = await listResponse.json() as { id: string };

    const createResponse = await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(stranger.accessToken),
      body: JSON.stringify({
        date: "2025-06-01",
        title: "Blocked Todo",
        status: "Open",
        todoListId: list.id
      })
    });

    expect(createResponse.status).toBe(400);
  });

  it("filters, orders, and pages todos using the .NET query shape", async () => {
    const user = await createVerifiedUser(app, bindings);
    const otherUser = await createVerifiedUser(app, bindings);

    const listResponse = await send(app, bindings, "http://localhost/api/todo-lists", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({ name: "Filter List" })
    });
    const list = await listResponse.json() as { id: string };

    for (const title of ["Alpha", "Zulu"]) {
      const response = await send(app, bindings, "http://localhost/api/todos", {
        method: "POST",
        headers: authHeaders(user.accessToken),
        body: JSON.stringify({
          date: "2025-02-01",
          title,
          status: "Completed",
          todoListId: list.id
        })
      });
      expect(response.status).toBe(200);
    }

    await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        date: "2025-02-01",
        title: "Wrong Status",
        status: "Open",
        todoListId: list.id
      })
    });

    await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(otherUser.accessToken),
      body: JSON.stringify({
        date: "2025-02-01",
        title: "Other User",
        status: "Completed"
      })
    });

    const response = await send(
      app,
      bindings,
      `http://localhost/api/todos?status=Completed&date=2025-02-01&todoListId=${list.id}&orderBy=Title&order=Desc&page=1&limit=1`,
      { headers: authHeaders(user.accessToken) }
    );

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ title: string }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(body.page).toBe(1);
    expect(body.limit).toBe(1);
    expect(body.total).toBe(2);
    expect(body.items).toHaveLength(1);
    expect(body.items[0]?.title).toBe("Zulu");
  });

  it("preserves null fields during patch and updates provided values", async () => {
    const user = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        date: "2025-08-01",
        title: "Keep My Title",
        status: "Open"
      })
    });
    const created = await createResponse.json() as { id: string };

    const updateResponse = await send(app, bindings, `http://localhost/api/todos/${created.id}`, {
      method: "PATCH",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        title: null,
        status: "Completed"
      })
    });

    expect(updateResponse.status).toBe(200);
    expect(await updateResponse.json()).toEqual({ id: created.id });

    const getResponse = await send(app, bindings, `http://localhost/api/todos/${created.id}`, {
      headers: authHeaders(user.accessToken)
    });

    expect(await getResponse.json()).toMatchObject({
      id: created.id,
      title: "Keep My Title",
      status: "Completed",
      date: "2025-08-01"
    });
  });

  it("deletes a todo and hides it from later reads", async () => {
    const user = await createVerifiedUser(app, bindings);

    const createResponse = await send(app, bindings, "http://localhost/api/todos", {
      method: "POST",
      headers: authHeaders(user.accessToken),
      body: JSON.stringify({
        date: "2025-09-01",
        title: "Delete Me",
        status: "Open"
      })
    });
    const created = await createResponse.json() as { id: string };

    const deleteResponse = await send(app, bindings, `http://localhost/api/todos/${created.id}`, {
      method: "DELETE",
      headers: authHeaders(user.accessToken)
    });

    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });

    const getResponse = await send(app, bindings, `http://localhost/api/todos/${created.id}`, {
      headers: authHeaders(user.accessToken)
    });
    expect(getResponse.status).toBe(404);
  });
});
