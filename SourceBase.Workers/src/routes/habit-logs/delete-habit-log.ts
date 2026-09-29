import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const guidSchema = z.string().regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, "The value is not valid.");

const paramsSchema = z.object({
  id: guidSchema.refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerDeleteHabitLogRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const log = await c.env.DB.prepare(
      `SELECT id, user_id
       FROM habit_logs
       WHERE id = ?`
    )
      .bind(params.id)
      .first<{ id: string; user_id: string }>();

    if (!log || log.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM habit_logs WHERE id = ?`).bind(log.id).run();
    return c.json({ success: true });
  });
}
