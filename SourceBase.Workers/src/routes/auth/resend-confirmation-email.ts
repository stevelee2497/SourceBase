import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { BadRequestError, NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateOtp, getOtpExpiryIso } from "../../lib/security";
import { insertEmail, nowIso } from "../../lib/db";

const requestSchema = z.object({
  email: z.email()
});

export function registerResendConfirmationEmailRoute(router: Hono<AppEnv>) {
  router.post("/resendConfirmationEmail", async c => {
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, email_confirmed
       FROM users
       WHERE lower(email) = lower(?)`
    )
      .bind(request.email)
      .first<{ id: string; user_name: string; email_confirmed: number }>();

    if (!user) throw new NotFoundError("User not found");
    if (user.email_confirmed === 1) throw new BadRequestError("Email already confirmed");

    const otp = generateOtp();
    await c.env.DB.prepare(
      `UPDATE users
       SET otp_code = ?, otp_code_expires_on = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(otp, getOtpExpiryIso(c.env), nowIso(), user.user_name, user.id)
      .run();

    await insertEmail(c.env.DB, request.email, "Confirm your email", `Your confirmation code is: <b>${otp}</b>`);
    return c.json({ success: true });
  });
}
