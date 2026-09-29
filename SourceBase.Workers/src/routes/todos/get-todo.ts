import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";

const paramsSchema = z.object({
  id: z.string().uuid().refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerGetTodoRoute(router: Hono<AppEnv>) {
  router.get("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const todo = await c.env.DB.prepare(
      `SELECT id, user_id, date, title, status, todo_list_id, created_on, created_by, updated_on, updated_by
       FROM todo_items
       WHERE id = ? AND user_id = ?`
    )
      .bind(params.id, currentUser.userId)
      .first<{
        id: string;
        user_id: string;
        date: string;
        title: string;
        status: string;
        todo_list_id: string | null;
        created_on: string | null;
        created_by: string | null;
        updated_on: string | null;
        updated_by: string | null;
      }>();

    if (!todo) throw new NotFoundError();

    return c.json({
      id: todo.id,
      userId: todo.user_id,
      date: todo.date,
      title: todo.title,
      status: todo.status,
      todoListId: todo.todo_list_id,
      createdOn: todo.created_on,
      createdBy: todo.created_by,
      updatedOn: todo.updated_on,
      updatedBy: todo.updated_by
    });
  });
}
