import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ForbiddenError, NotFoundError, ValidationError } from "../../lib/errors";

export function registerDeleteCategoryRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const id = c.req.param("id");
    const category = await c.env.DB.prepare(
      `SELECT id, user_id, is_system
       FROM categories
       WHERE id = ?`
    )
      .bind(id)
      .first<{
        id: string;
        user_id: string | null;
        is_system: number;
      }>();

    if (!category) throw new NotFoundError();
    if (category.is_system === 1) throw new ForbiddenError();
    if (category.user_id !== currentUser.userId) throw new NotFoundError();

    const isInUse = await c.env.DB.prepare(`SELECT id FROM transactions WHERE category_id = ? LIMIT 1`)
      .bind(category.id)
      .first<{ id: string }>();

    if (isInUse) throw new ValidationError(undefined, "Category is in use by transactions");

    await c.env.DB.prepare(`DELETE FROM categories WHERE id = ?`).bind(category.id).run();
    return c.json({ success: true });
  });
}
