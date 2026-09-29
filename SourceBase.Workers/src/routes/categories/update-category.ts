import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ForbiddenError, NotFoundError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(100).nullable().optional(),
  icon: z.string().nullable().optional()
});

export function registerUpdateCategoryRoute(router: Hono<AppEnv>) {
  router.patch("/:id", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = c.req.param("id");
    const category = await c.env.DB.prepare(
      `SELECT id, name, icon, user_id, is_system
       FROM categories
       WHERE id = ?`
    )
      .bind(id)
      .first<{
        id: string;
        name: string;
        icon: string | null;
        user_id: string | null;
        is_system: number;
      }>();

    if (!category) throw new NotFoundError();
    if (category.is_system === 1) throw new ForbiddenError();
    if (category.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE categories
       SET name = ?, icon = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.name ?? category.name, request.icon ?? category.icon, nowIso(), currentUser.userName, category.id)
      .run();

    return c.json({ id: category.id });
  });
}
