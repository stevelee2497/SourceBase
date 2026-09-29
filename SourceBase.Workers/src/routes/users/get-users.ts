import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { roles } from "../../lib/db";
import type { AppEnv } from "../../types";

const querySchema = z.object({
  page: z.coerce.number().int().positive().default(1),
  limit: z.coerce.number().int().positive().default(10),
  order: z.enum(["Asc", "Desc"]).default("Asc"),
  orderBy: z
    .enum(["UserName", "Email", "FirstName", "LastName", "PhoneNumber", "EmailConfirmed", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy"])
    .default("CreatedOn")
});

const orderByColumns = {
  UserName: "users.user_name COLLATE NOCASE",
  Email: "users.email COLLATE NOCASE",
  FirstName: "users.first_name COLLATE NOCASE",
  LastName: "users.last_name COLLATE NOCASE",
  PhoneNumber: "users.phone_number COLLATE NOCASE",
  EmailConfirmed: "users.email_confirmed",
  CreatedOn: "users.created_on",
  CreatedBy: "users.created_by COLLATE NOCASE",
  UpdatedOn: "users.updated_on",
  UpdatedBy: "users.updated_by COLLATE NOCASE"
} as const;

interface UserRow {
  id: string;
  user_name: string | null;
  email: string | null;
  first_name: string | null;
  last_name: string | null;
  phone_number: string | null;
  email_confirmed: number;
  avatar_url: string | null;
}

export function registerGetUsersRoute(router: Hono<AppEnv>) {
  router.get("/", requireRole(roles.admin), async c => {
    const query = querySchema.parse(Object.fromEntries(new URL(c.req.url).searchParams.entries()));
    const orderBy = orderByColumns[query.orderBy];
    const direction = query.order === "Asc" ? "ASC" : "DESC";
    const offset = (query.page - 1) * query.limit;

    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS count FROM users`).first<number>("count");
    const users = await c.env.DB.prepare(
      `SELECT users.id, users.user_name, users.email, users.first_name, users.last_name, users.phone_number, users.email_confirmed, users.avatar_url
       FROM users
       ORDER BY ${orderBy} ${direction}, users.id ASC
       LIMIT ? OFFSET ?`
    )
      .bind(query.limit, offset)
      .all<UserRow>();

    const rolesByUserId = new Map<string, string[]>();
    if (users.results.length > 0) {
      const placeholders = users.results.map(() => "?").join(", ");
      const userRoles = await c.env.DB.prepare(
        `SELECT user_roles.user_id, roles.name
         FROM user_roles
         INNER JOIN roles ON roles.id = user_roles.role_id
         WHERE user_roles.user_id IN (${placeholders})
         ORDER BY roles.name`
      )
        .bind(...users.results.map(user => user.id))
        .all<{ user_id: string; name: string }>();

      for (const userRole of userRoles.results) {
        const current = rolesByUserId.get(userRole.user_id) ?? [];
        current.push(userRole.name);
        rolesByUserId.set(userRole.user_id, current);
      }
    }

    return c.json({
      items: users.results.map(user => ({
        id: user.id,
        userName: user.user_name,
        email: user.email,
        firstName: user.first_name,
        lastName: user.last_name,
        phoneNumber: user.phone_number,
        emailConfirmed: user.email_confirmed === 1,
        avatarUrl: user.avatar_url,
        roles: rolesByUserId.get(user.id) ?? []
      })),
      page: query.page,
      limit: query.limit,
      total: total ?? 0
    });
  });
}
