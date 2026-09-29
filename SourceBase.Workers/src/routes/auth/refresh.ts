import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { UnauthorizedError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { createLoginResponse, verifyRefreshToken } from "../../lib/security";
import { getUserRoles } from "../../lib/db";

const requestSchema = z.object({
  token: z.string().min(1)
});

export function registerRefreshRoute(router: Hono<AppEnv>) {
  router.post("/refresh", async c => {
    const request = await readJson(c, requestSchema);
    const refreshPrincipal = await verifyRefreshToken(c.env, request.token);
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, email, email_confirmed, security_stamp
       FROM users
       WHERE id = ?`
    )
      .bind(refreshPrincipal.userId)
      .first<{
        id: string;
        user_name: string;
        email: string;
        email_confirmed: number;
        security_stamp: string;
      }>();

    if (!user || user.email_confirmed !== 1 || user.security_stamp !== refreshPrincipal.securityStamp)
      throw new UnauthorizedError("User not found");

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
