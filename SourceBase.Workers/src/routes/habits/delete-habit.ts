import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ForbiddenError, NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const guidSchema = z.string().regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, "The value is not valid.");

const paramsSchema = z.object({
  id: guidSchema.refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerDeleteHabitRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const habit = await c.env.DB.prepare(
      `SELECT id, user_id, is_system
       FROM habits
       WHERE id = ?`
    )
      .bind(params.id)
      .first<{ id: string; user_id: string | null; is_system: number }>();

    if (!habit) throw new NotFoundError();
    if (habit.is_system === 1) throw new ForbiddenError();
    if (habit.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM habits WHERE id = ?`).bind(habit.id).run();
    return c.json({ success: true });
  });
}
