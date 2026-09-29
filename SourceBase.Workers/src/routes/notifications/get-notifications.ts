import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const optionalPositiveInt = z.preprocess(value => (value === undefined ? undefined : Number(value)), z.number().int().positive().optional());
const querySchema = z.object({
  unreadOnly: z.enum(["true", "false"]).optional(),
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["CreatedOn", "Title", "IsRead"]).optional()
});

const orderByColumns = {
  CreatedOn: "created_on",
  Title: "title",
  IsRead: "is_read"
} as const;

export function registerGetNotificationsRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 10;
    const where = ["user_id = ?"];
    const params: Array<string | number> = [currentUser.userId];

    if (query.unreadOnly === "true") where.push("is_read = 0");

    const whereClause = where.join(" AND ");
    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM notifications WHERE ${whereClause}`)
      .bind(...params)
      .first<number>("value") ?? 0;

    const orderBy = query.orderBy ?? "CreatedOn";
    const order = query.order ?? "Desc";
    const orderClause = ` ORDER BY ${orderByColumns[orderBy]} ${order === "Desc" ? "DESC" : "ASC"}, id ${order === "Desc" ? "DESC" : "ASC"}`;
    const rows = await c.env.DB.prepare(
      `SELECT id, title, message, is_read, created_on
       FROM notifications
       WHERE ${whereClause}${orderClause}
       LIMIT ? OFFSET ?`
    )
      .bind(...params, limit, (page - 1) * limit)
      .all<{
        id: string;
        title: string;
        message: string;
        is_read: number;
        created_on: string | null;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        title: row.title,
        message: row.message,
        isRead: row.is_read === 1,
        createdOn: row.created_on
      })),
      page,
      limit,
      total: Number(total)
    });
  });
}
