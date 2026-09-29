import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import type { AppEnv } from "../../types";

export function registerMarkAllNotificationsAsReadRoute(router: Hono<AppEnv>) {
  router.put("/read-all", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    await c.env.DB.prepare(
      `UPDATE notifications
       SET is_read = 1, updated_on = ?, updated_by = ?
       WHERE user_id = ? AND is_read = 0`
    )
      .bind(nowIso(), currentUser.userName, currentUser.userId)
      .run();

    return c.json({ success: true });
  });
}
