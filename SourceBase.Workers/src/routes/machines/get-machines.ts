import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const optionalPositiveInt = z.preprocess(value => (value === undefined ? undefined : Number(value)), z.number().int().positive().optional());
const querySchema = z.object({
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["Name", "Status", "LastReportedOn", "CreatedOn", "UpdatedOn"]).optional()
});

const orderByColumns = {
  Name: "name",
  Status: "status",
  LastReportedOn: "last_reported_on",
  CreatedOn: "created_on",
  UpdatedOn: "updated_on"
} as const;

export function registerGetMachinesRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 10;
    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM machines WHERE user_id = ?`)
      .bind(currentUser.userId)
      .first<number>("value") ?? 0;

    const orderClause = query.orderBy
      ? ` ORDER BY ${orderByColumns[query.orderBy]} ${query.order === "Desc" ? "DESC" : "ASC"}`
      : "";

    const rows = await c.env.DB.prepare(
      `SELECT id, name, alias, status, last_reported_on
       FROM machines
       WHERE user_id = ?${orderClause}
       LIMIT ? OFFSET ?`
    )
      .bind(currentUser.userId, limit, (page - 1) * limit)
      .all<{
        id: string;
        name: string;
        alias: string | null;
        status: string;
        last_reported_on: string | null;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        name: row.name,
        alias: row.alias,
        status: row.status,
        lastReportedOn: row.last_reported_on
      })),
      page,
      limit,
      total: Number(total)
    });
  });
}
