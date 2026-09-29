import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const emptyGuid = "00000000-0000-0000-0000-000000000000";

const paramsSchema = z.object({
  id: z.string().uuid().refine(value => value !== emptyGuid, "The value is not valid.")
});

const requestSchema = z.object({
  name: z.string().min(1).max(200).nullable().optional()
});

export function registerUpdateTodoListRoute(router: Hono<AppEnv>) {
  router.patch("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const request = await readJson(c, requestSchema);
    const list = await c.env.DB.prepare(
      `SELECT id, name
       FROM todo_lists
       WHERE id = ? AND user_id = ?`
    )
      .bind(params.id, currentUser.userId)
      .first<{ id: string; name: string }>();

    if (!list) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE todo_lists
       SET name = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.name ?? list.name, nowIso(), currentUser.userName, list.id)
      .run();

    return c.json({ id: list.id });
  });
}
