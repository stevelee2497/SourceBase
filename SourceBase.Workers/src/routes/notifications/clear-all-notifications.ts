import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

export function registerClearAllNotificationsRoute(router: Hono<AppEnv>) {
  router.delete("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    await c.env.DB.prepare(`DELETE FROM notifications WHERE user_id = ?`).bind(currentUser.userId).run();
    return c.json({ success: true });
  });
}
