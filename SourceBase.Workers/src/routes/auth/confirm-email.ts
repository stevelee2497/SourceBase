import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { roles } from "../../lib/db";
import { ApiInternalError, UnauthorizedError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { nowIso } from "../../lib/db";

const requestSchema = z.object({
  email: z.email(),
  code: z.string().length(6)
});

export function registerConfirmEmailRoute(router: Hono<AppEnv>) {
  router.post("/confirmEmail", async c => {
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, otp_code, otp_code_expires_on
       FROM users
       WHERE lower(email) = lower(?)`
    )
      .bind(request.email)
      .first<{ id: string; user_name: string; otp_code: string | null; otp_code_expires_on: string | null }>();

    if (!user) throw new UnauthorizedError("Invalid credentials");
    if (user.otp_code !== request.code || !user.otp_code_expires_on || user.otp_code_expires_on <= nowIso())
      throw new UnauthorizedError("Invalid or expired code");

    const userRole = await c.env.DB.prepare(`SELECT id FROM roles WHERE name = ?`).bind(roles.user).first<{ id: string }>();
    if (!userRole) throw new ApiInternalError("Default user role not found");

    await c.env.DB.prepare(
      `UPDATE users
       SET email_confirmed = 1, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(nowIso(), user.user_name, user.id)
      .run();

    await c.env.DB.prepare(`INSERT OR IGNORE INTO user_roles (user_id, role_id) VALUES (?, ?)`)
      .bind(user.id, userRole.id)
      .run();

    return c.json({ success: true });
  });
}
