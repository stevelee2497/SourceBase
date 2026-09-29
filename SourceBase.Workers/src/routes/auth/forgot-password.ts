import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { insertEmail, nowIso } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateOtp, getOtpExpiryIso } from "../../lib/security";

const requestSchema = z.object({
  email: z.email()
});

export function registerForgotPasswordRoute(router: Hono<AppEnv>) {
  router.post("/forgotPassword", async c => {
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name
       FROM users
       WHERE lower(email) = lower(?)`
    )
      .bind(request.email)
      .first<{ id: string; user_name: string }>();

    if (!user) throw new NotFoundError("User not found");

    const otp = generateOtp();
    await c.env.DB.prepare(
      `UPDATE users
       SET otp_code = ?, otp_code_expires_on = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(otp, getOtpExpiryIso(c.env), nowIso(), user.user_name, user.id)
      .run();

    await insertEmail(c.env.DB, request.email, "Reset Password", `Your password reset code is: <b>${otp}</b>`);
    return c.json({ success: true });
  });
}
