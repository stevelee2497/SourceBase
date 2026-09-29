import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { BadRequestError, NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateSecurityStamp, hashPassword } from "../../lib/security";
import { nowIso } from "../../lib/db";

const requestSchema = z.object({
  email: z.email(),
  code: z.string().length(6),
  newPassword: z.string().min(6)
});

export function registerResetPasswordRoute(router: Hono<AppEnv>) {
  router.post("/resetPassword", async c => {
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, otp_code, otp_code_expires_on
       FROM users
       WHERE lower(email) = lower(?)`
    )
      .bind(request.email)
      .first<{ id: string; user_name: string; otp_code: string | null; otp_code_expires_on: string | null }>();

    if (!user) throw new NotFoundError("User not found");
    if (user.otp_code !== request.code || !user.otp_code_expires_on || user.otp_code_expires_on <= nowIso())
      throw new BadRequestError("Invalid or expired code");

    await c.env.DB.prepare(
      `UPDATE users
       SET otp_code = NULL,
           otp_code_expires_on = NULL,
           email_confirmed = 1,
           password_hash = ?,
           security_stamp = ?,
           updated_on = ?,
           updated_by = ?
       WHERE id = ?`
    )
      .bind(await hashPassword(request.newPassword), generateSecurityStamp(), nowIso(), user.user_name, user.id)
      .run();

    return c.json({ success: true });
  });
}
