import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerMarkNotificationAsReadRoute(router: Hono<AppEnv>) {
  router.put("/:id/read", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const notification = await c.env.DB.prepare(
      `SELECT id
       FROM notifications
       WHERE id = ? AND user_id = ?`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{ id: string }>();

    if (!notification) throw new NotFoundError("Notification not found.");

    await c.env.DB.prepare(
      `UPDATE notifications
       SET is_read = 1, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(nowIso(), currentUser.userName, notification.id)
      .run();

    return c.json({ success: true });
  });
}
