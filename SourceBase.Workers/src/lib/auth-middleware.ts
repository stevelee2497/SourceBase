import type { MiddlewareHandler } from "hono";
import { getUserRoles } from "./db";
import { ForbiddenError, UnauthorizedError } from "./errors";
import { verifyAccessToken } from "./security";
import type { AppEnv } from "../types";

export const requireAuth: MiddlewareHandler<AppEnv> = async (c, next) => {
  const header = c.req.header("Authorization");
  if (!header?.startsWith("Bearer ")) throw new UnauthorizedError("User is not authorized");

  const token = header.slice("Bearer ".length).trim();
  const payload = await verifyAccessToken(c.env, token);
  const user = await c.env.DB.prepare(
    `SELECT id, user_name, email, email_confirmed, security_stamp
     FROM users
     WHERE id = ?`
  )
    .bind(payload.userId)
    .first<{
      id: string;
      user_name: string;
      email: string;
      email_confirmed: number;
      security_stamp: string;
    }>();

  if (!user || user.email_confirmed !== 1 || user.security_stamp !== payload.securityStamp)
    throw new UnauthorizedError("User is not authorized");

  c.set("currentUser", {
    userId: user.id,
    userName: payload.userName,
    email: payload.email,
    roles: await getUserRoles(c.env.DB, user.id),
    securityStamp: user.security_stamp
  });

  await next();
};

export function requireRole(role: string): MiddlewareHandler<AppEnv> {
  return async (c, next) => {
    await requireAuth(c, async () => {});

    const currentUser = c.get("currentUser");
    if (!currentUser.roles.some(currentRole => currentRole.localeCompare(role, undefined, { sensitivity: "accent" }) === 0))
      throw new ForbiddenError("User is forbidden");

    await next();
  };
}
