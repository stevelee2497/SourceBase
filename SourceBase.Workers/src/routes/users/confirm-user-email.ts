import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { nowIso, roles } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerConfirmUserEmailRoute(router: Hono<AppEnv>) {
  router.post("/:id/confirm-email", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const id = z.string().uuid().parse(c.req.param("id"));
    const userId = await c.env.DB.prepare(`SELECT id FROM users WHERE id = ?`).bind(id).first<string>("id");
    if (!userId) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE users
       SET email_confirmed = 1, otp_code = NULL, otp_code_expires_on = NULL, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(nowIso(), currentUser.userName, id)
      .run();

    return c.json({ success: true });
  });
}
