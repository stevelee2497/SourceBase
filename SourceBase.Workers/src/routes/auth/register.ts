import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { BadRequestError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateOtp, generateSecurityStamp, getOtpExpiryIso, hashPassword } from "../../lib/security";
import { insertEmail, nowIso } from "../../lib/db";

const requestSchema = z.object({
  userName: z.string().min(1).max(256),
  email: z.email(),
  password: z.string().min(6),
  firstName: z.string().optional().default(""),
  lastName: z.string().optional().default("")
});

export function registerRegisterRoute(router: Hono<AppEnv>) {
  router.post("/register", async c => {
    const request = await readJson(c, requestSchema);
    const duplicate = await c.env.DB.prepare(
      `SELECT id
       FROM users
       WHERE lower(user_name) = lower(?)
          OR lower(email) = lower(?)`
    )
      .bind(request.userName, request.email)
      .first<{ id: string }>();

    if (duplicate) throw new BadRequestError("Username or email is already taken.");

    const now = nowIso();
    const otpCode = generateOtp();
    const userId = crypto.randomUUID();
    await c.env.DB.prepare(
      `INSERT INTO users (
         id, user_name, email, email_confirmed, password_hash, security_stamp,
         first_name, last_name, otp_code, otp_code_expires_on, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        userId,
        request.userName,
        request.email,
        0,
        await hashPassword(request.password),
        generateSecurityStamp(),
        request.firstName || null,
        request.lastName || null,
        otpCode,
        getOtpExpiryIso(c.env),
        now,
        request.userName
      )
      .run();

    await insertEmail(c.env.DB, request.email, "Confirm your email", `Your confirmation code is: <b>${otpCode}</b>`);
    return c.json({ id: userId });
  });
}
