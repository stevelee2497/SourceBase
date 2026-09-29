import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { UnauthorizedError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { createLoginResponse, verifyPassword } from "../../lib/security";
import { getUserRoles } from "../../lib/db";

const requestSchema = z.object({
  email: z.email(),
  password: z.string().min(1)
});

export function registerLoginRoute(router: Hono<AppEnv>) {
  router.post("/login", async c => {
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, email, email_confirmed, password_hash, security_stamp
       FROM users
       WHERE lower(email) = lower(?)`
    )
      .bind(request.email)
      .first<{
        id: string;
        user_name: string;
        email: string;
        email_confirmed: number;
        password_hash: string;
        security_stamp: string;
      }>();

    if (!user || user.email_confirmed !== 1 || !(await verifyPassword(user.password_hash, request.password)))
      throw new UnauthorizedError("Invalid credentials");

    return c.json(
      await createLoginResponse(c.env, {
        userId: user.id,
        userName: user.user_name,
        email: user.email,
        roles: await getUserRoles(c.env.DB, user.id),
        securityStamp: user.security_stamp
      })
    );
  });
}
