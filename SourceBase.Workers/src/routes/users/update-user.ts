import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { insertEmail, nowIso, roles } from "../../lib/db";
import { BadRequestError, NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import { generateOtp, generateSecurityStamp, getOtpExpiryIso } from "../../lib/security";
import type { AppEnv } from "../../types";

const requestSchema = z.object({
  email: z.email(),
  firstName: z.string().max(100).nullable().optional(),
  lastName: z.string().max(100).nullable().optional(),
  phoneNumber: z.string().max(20).nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  roles: z.array(z.string().min(1).max(256)).nullable().optional()
});

interface UserRecord {
  id: string;
  email: string;
  email_confirmed: number;
  otp_code: string | null;
  otp_code_expires_on: string | null;
  security_stamp: string;
}

export function registerUpdateUserRoute(router: Hono<AppEnv>) {
  router.put("/:id", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const id = z.string().uuid().parse(c.req.param("id"));
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, email, email_confirmed, otp_code, otp_code_expires_on, security_stamp
       FROM users
       WHERE id = ?`
    )
      .bind(id)
      .first<UserRecord>();

    if (!user) throw new NotFoundError();

    const duplicateEmail = await c.env.DB.prepare(`SELECT id FROM users WHERE id != ? AND lower(email) = lower(?)`)
      .bind(id, request.email)
      .first<string>("id");
    if (duplicateEmail) throw new BadRequestError("Email is already taken.");

    const emailChanged = user.email.localeCompare(request.email, undefined, { sensitivity: "accent" }) !== 0;
    let emailConfirmed = user.email_confirmed;
    let otpCode = user.otp_code;
    let otpCodeExpiresOn = user.otp_code_expires_on;
    let securityStamp = user.security_stamp;

    if (emailChanged) {
      emailConfirmed = 0;
      otpCode = generateOtp();
      otpCodeExpiresOn = getOtpExpiryIso(c.env);
    }

    let rolesChanged = false;
    if (request.roles && request.roles.length > 0) {
      const normalizedRoles = normalizeValues(request.roles);
      const desiredRoles = await getRolesByNames(c.env.DB, normalizedRoles);
      if (desiredRoles.length !== normalizedRoles.length) throw new BadRequestError("One or more specified roles do not exist.");

      const existingRoles = await c.env.DB.prepare(
        `SELECT roles.id, roles.name
         FROM roles
         INNER JOIN user_roles ON user_roles.role_id = roles.id
         WHERE user_roles.user_id = ?`
      )
        .bind(user.id)
        .all<{ id: string; name: string }>();

      const existingRoleNames = new Set(existingRoles.results.map(role => role.name.toLowerCase()));
      const desiredRoleNames = new Set(desiredRoles.map(role => role.name.toLowerCase()));
      const rolesToRemove = existingRoles.results.filter(role => !desiredRoleNames.has(role.name.toLowerCase()));
      const rolesToAdd = desiredRoles.filter(role => !existingRoleNames.has(role.name.toLowerCase()));

      for (const role of rolesToRemove)
        await c.env.DB.prepare(`DELETE FROM user_roles WHERE user_id = ? AND role_id = ?`).bind(user.id, role.id).run();

      for (const role of rolesToAdd)
        await c.env.DB.prepare(`INSERT INTO user_roles (user_id, role_id) VALUES (?, ?)`).bind(user.id, role.id).run();

      rolesChanged = rolesToRemove.length > 0 || rolesToAdd.length > 0;
    }

    if (emailChanged || rolesChanged) securityStamp = generateSecurityStamp();

    await c.env.DB.prepare(
      `UPDATE users
       SET email = ?,
           email_confirmed = ?,
           first_name = ?,
           last_name = ?,
           phone_number = ?,
           avatar_url = ?,
           otp_code = ?,
           otp_code_expires_on = ?,
           security_stamp = ?,
           updated_on = ?,
           updated_by = ?
       WHERE id = ?`
    )
      .bind(
        request.email,
        emailConfirmed,
        request.firstName ?? null,
        request.lastName ?? null,
        request.phoneNumber ?? null,
        request.avatarUrl ?? null,
        otpCode,
        otpCodeExpiresOn,
        securityStamp,
        nowIso(),
        currentUser.userName,
        user.id
      )
      .run();

    if (emailChanged)
      await insertEmail(c.env.DB, request.email, "Confirm your email", `Your confirmation code is: <b>${otpCode}</b>`);

    return c.json({ id: user.id });
  });
}

async function getRolesByNames(db: AppEnv["Bindings"]["DB"], roleNames: string[]) {
  const placeholders = roleNames.map(() => "?").join(", ");
  const matchingRoles = await db.prepare(`SELECT id, name FROM roles WHERE lower(name) IN (${placeholders})`)
    .bind(...roleNames.map(roleName => roleName.toLowerCase()))
    .all<{ id: string; name: string }>();

  const rolesByName = new Map(matchingRoles.results.map(role => [role.name.toLowerCase(), role]));
  return roleNames.map(roleName => rolesByName.get(roleName.toLowerCase())).filter((role): role is { id: string; name: string } => Boolean(role));
}

function normalizeValues(values: string[]) {
  return [...new Map(values.map(value => [value.trim().toLowerCase(), value.trim()])).values()];
}
