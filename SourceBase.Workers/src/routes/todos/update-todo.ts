import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const todoStatuses = ["Open", "Completed", "Archived"] as const;
const isoDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => !Number.isNaN(Date.parse(`${value}T00:00:00.000Z`)), "Invalid date.");

const paramsSchema = z.object({
  id: z.string().uuid().refine(value => value !== emptyGuid, "The value is not valid.")
});

const requestSchema = z.object({
  date: isoDateSchema.nullable().optional(),
  title: z.string().min(1).nullable().optional(),
  status: z.enum(todoStatuses).nullable().optional()
});

export function registerUpdateTodoRoute(router: Hono<AppEnv>) {
  router.patch("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const request = await readJson(c, requestSchema);
    const todo = await c.env.DB.prepare(
      `SELECT id, title, date, status
       FROM todo_items
       WHERE id = ? AND user_id = ?`
    )
      .bind(params.id, currentUser.userId)
      .first<{ id: string; title: string; date: string; status: string }>();

    if (!todo) throw new NotFoundError();

    const updatedOn = nowIso();
    const nextTitle = request.title ?? todo.title;
    const nextDate = request.date ?? todo.date;
    const nextStatus = request.status ?? todo.status;

    await c.env.DB.prepare(
      `UPDATE todo_items
       SET title = ?, date = ?, status = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(nextTitle, nextDate, nextStatus, updatedOn, currentUser.userName, todo.id)
      .run();

    await c.env.DB.prepare(
      `INSERT INTO notifications (id, user_id, event, title, message, data, is_read, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        crypto.randomUUID(),
        currentUser.userId,
        "TodoUpdatedEvent",
        "Todo item updated",
        `Your todo item ${nextTitle} has been updated.`,
        JSON.stringify({ id: todo.id, title: nextTitle, date: nextDate, status: nextStatus }),
        1,
        updatedOn,
        currentUser.userName
      )
      .run();

    return c.json({ id: todo.id });
  });
}
