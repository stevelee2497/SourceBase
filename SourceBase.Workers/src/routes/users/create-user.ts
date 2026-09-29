import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { insertEmail, nowIso, roles } from "../../lib/db";
import { BadRequestError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateOtp, generateSecurityStamp, getOtpExpiryIso, hashPassword } from "../../lib/security";
import type { AppEnv } from "../../types";

const requestSchema = z.object({
  userName: z.string().min(1).max(256),
  email: z.email(),
  password: z.string().min(6),
  firstName: z.string().max(100).nullable().optional(),
  lastName: z.string().max(100).nullable().optional(),
  phoneNumber: z.string().max(20).nullable().optional(),
  roles: z.array(z.string().min(1).max(256)).nullable().optional()
});

export function registerCreateUserRoute(router: Hono<AppEnv>) {
  router.post("/", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);

    const duplicateUserName = await c.env.DB.prepare(`SELECT id FROM users WHERE lower(user_name) = lower(?)`)
      .bind(request.userName)
      .first<string>("id");
    if (duplicateUserName) throw new BadRequestError("Username is already taken.");

    const duplicateEmail = await c.env.DB.prepare(`SELECT id FROM users WHERE lower(email) = lower(?)`)
      .bind(request.email)
      .first<string>("id");
    if (duplicateEmail) throw new BadRequestError("Email is already taken.");

    const normalizedRoles = request.roles ? normalizeValues(request.roles) : null;
    const roleIds = normalizedRoles && normalizedRoles.length > 0 ? await getRoleIds(c.env.DB, normalizedRoles) : [];
    if (normalizedRoles && normalizedRoles.length > 0 && roleIds.length !== normalizedRoles.length)
      throw new BadRequestError("One or more specified roles do not exist.");

    const userId = crypto.randomUUID();
    const now = nowIso();
    const otpCode = generateOtp();
    await c.env.DB.prepare(
      `INSERT INTO users (
         id, user_name, email, email_confirmed, password_hash, security_stamp,
         phone_number, first_name, last_name, otp_code, otp_code_expires_on, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        userId,
        request.userName,
        request.email,
        0,
        await hashPassword(request.password),
        generateSecurityStamp(),
        request.phoneNumber ?? null,
        request.firstName ?? null,
        request.lastName ?? null,
        otpCode,
        getOtpExpiryIso(c.env),
        now,
        currentUser.userName
      )
      .run();

    for (const roleId of roleIds)
      await c.env.DB.prepare(`INSERT INTO user_roles (user_id, role_id) VALUES (?, ?)`).bind(userId, roleId).run();

    await insertEmail(c.env.DB, request.email, "Confirm your email", `Your confirmation code is: <b>${otpCode}</b>`);

    const adminUsers = await c.env.DB.prepare(
      `SELECT users.id
       FROM users
       INNER JOIN user_roles ON user_roles.user_id = users.id
       INNER JOIN roles ON roles.id = user_roles.role_id
       WHERE roles.name = ?`
    )
      .bind(roles.admin)
      .all<{ id: string }>();

    for (const adminUser of adminUsers.results) {
      await c.env.DB.prepare(
        `INSERT INTO notifications (id, user_id, event, title, message, data, is_read, created_on, created_by)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`
      )
        .bind(
          crypto.randomUUID(),
          adminUser.id,
          "GlobalNotificationEvent",
          "New User Registered",
          `A new user ${request.email} has been registered.`,
          "",
          0,
          now,
          currentUser.userName
        )
        .run();
    }

    return c.json({ id: userId });
  });
}

async function getRoleIds(db: AppEnv["Bindings"]["DB"], roleNames: string[]) {
  const placeholders = roleNames.map(() => "?").join(", ");
  const matchingRoles = await db.prepare(`SELECT id, name FROM roles WHERE lower(name) IN (${placeholders})`)
    .bind(...roleNames.map(roleName => roleName.toLowerCase()))
    .all<{ id: string; name: string }>();

  const roleIdsByName = new Map(matchingRoles.results.map(role => [role.name.toLowerCase(), role.id]));
  return roleNames.map(roleName => roleIdsByName.get(roleName.toLowerCase())).filter((roleId): roleId is string => Boolean(roleId));
}

function normalizeValues(values: string[]) {
  return [...new Map(values.map(value => [value.trim().toLowerCase(), value.trim()])).values()];
}
