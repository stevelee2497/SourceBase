import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { BadRequestError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const todoStatuses = ["Open", "Completed", "Archived"] as const;

const isoDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => !Number.isNaN(Date.parse(`${value}T00:00:00.000Z`)), "Invalid date.");

const requestSchema = z.object({
  date: isoDateSchema,
  title: z.string().min(1),
  status: z.enum(todoStatuses).default("Open"),
  todoListId: z.string().uuid().nullable().optional()
});

export function registerCreateTodoRoute(router: Hono<AppEnv>) {
  router.post("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);

    if (request.todoListId) {
      const listExists = await c.env.DB.prepare(
        `SELECT id
         FROM todo_lists
         WHERE id = ? AND user_id = ?`
      )
        .bind(request.todoListId, currentUser.userId)
        .first<{ id: string }>();

      if (!listExists) throw new BadRequestError("Todo list not found.");
    }

    const id = crypto.randomUUID();
    const createdOn = nowIso();

    await c.env.DB.prepare(
      `INSERT INTO todo_items (id, title, date, status, user_id, todo_list_id, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(id, request.title, request.date, request.status, currentUser.userId, request.todoListId ?? null, createdOn, currentUser.userName)
      .run();

    await c.env.DB.prepare(
      `INSERT INTO notifications (id, user_id, event, title, message, data, is_read, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        crypto.randomUUID(),
        currentUser.userId,
        "TodoCreatedEvent",
        "Todo item created",
        `A new todo item ${request.title} has been created.`,
        JSON.stringify({ id, title: request.title, date: request.date, status: request.status, todoListId: request.todoListId ?? null }),
        1,
        createdOn,
        currentUser.userName
      )
      .run();

    return c.json({ id });
  });
}
