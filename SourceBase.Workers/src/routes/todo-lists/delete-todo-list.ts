import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";

const paramsSchema = z.object({
  id: z.string().uuid().refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerDeleteTodoListRoute(router: Hono<AppEnv>) {
  router.delete("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const list = await c.env.DB.prepare(
      `SELECT id
       FROM todo_lists
       WHERE id = ? AND user_id = ?`
    )
      .bind(params.id, currentUser.userId)
      .first<{ id: string }>();

    if (!list) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM todo_lists WHERE id = ?`).bind(list.id).run();
    return c.json({ success: true });
  });
}
