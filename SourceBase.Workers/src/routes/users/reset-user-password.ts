import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { insertEmail, nowIso, roles } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateSecurityStamp, hashPassword } from "../../lib/security";
import type { AppEnv } from "../../types";

const requestSchema = z.object({
  newPassword: z.string().min(6)
});

export function registerResetUserPasswordRoute(router: Hono<AppEnv>) {
  router.post("/:id/reset-password", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const id = z.string().uuid().parse(c.req.param("id"));
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(`SELECT id, email FROM users WHERE id = ?`).bind(id).first<{ id: string; email: string }>();
    if (!user) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE users
       SET password_hash = ?, security_stamp = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(await hashPassword(request.newPassword), generateSecurityStamp(), nowIso(), currentUser.userName, id)
      .run();

    await insertEmail(
      c.env.DB,
      user.email,
      "Your password has been reset",
      `Your account password has been reset by an administrator. Your new password is: <b>${request.newPassword}</b><br/>Please change it after logging in.`
    );

    return c.json({ success: true });
  });
}
