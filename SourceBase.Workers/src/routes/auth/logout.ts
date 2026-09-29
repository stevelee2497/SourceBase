import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { generateSecurityStamp } from "../../lib/security";
import { nowIso } from "../../lib/db";

export function registerLogoutRoute(router: Hono<AppEnv>) {
  router.post("/logout", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    await c.env.DB.prepare(
      `UPDATE users
       SET security_stamp = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(generateSecurityStamp(), nowIso(), currentUser.userName, currentUser.userId)
      .run();

    return c.json({ success: true });
  });
}
