import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ForbiddenError, NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const guidSchema = z.string().regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, "The value is not valid.");

const paramsSchema = z.object({
  id: guidSchema.refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerDeleteIconRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const icon = await c.env.DB.prepare(
      `SELECT id, is_system
       FROM icons
       WHERE id = ?`
    )
      .bind(params.id)
      .first<{ id: string; is_system: number }>();

    if (!icon) throw new NotFoundError();
    if (icon.is_system === 1) throw new ForbiddenError();

    await c.env.DB.prepare(`DELETE FROM icons WHERE id = ?`).bind(icon.id).run();
    return c.json({ success: true });
  });
}
