import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";

const querySchema = z.object({
  page: z.coerce.number().int().positive().default(1),
  limit: z.coerce.number().int().positive().default(10),
  order: z.enum(["Asc", "Desc"]).default("Asc"),
  orderBy: z.enum(["Name", "Description", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy"]).default("CreatedOn")
});

const orderByColumns = {
  Name: "name COLLATE NOCASE",
  Description: "description COLLATE NOCASE",
  CreatedOn: "created_on",
  CreatedBy: "created_by COLLATE NOCASE",
  UpdatedOn: "updated_on",
  UpdatedBy: "updated_by COLLATE NOCASE"
} as const;

export function registerGetRolesRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const query = querySchema.parse(Object.fromEntries(new URL(c.req.url).searchParams.entries()));
    const orderBy = orderByColumns[query.orderBy];
    const direction = query.order === "Asc" ? "ASC" : "DESC";
    const offset = (query.page - 1) * query.limit;

    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS count FROM roles`).first<number>("count");
    const roleRows = await c.env.DB.prepare(
      `SELECT id, name, description
       FROM roles
       ORDER BY ${orderBy} ${direction}, id ASC
       LIMIT ? OFFSET ?`
    )
      .bind(query.limit, offset)
      .all<{ id: string; name: string; description: string }>();

    return c.json({
      items: roleRows.results.map(role => ({ id: role.id, name: role.name, description: role.description })),
      page: query.page,
      limit: query.limit,
      total: total ?? 0
    });
  });
}
