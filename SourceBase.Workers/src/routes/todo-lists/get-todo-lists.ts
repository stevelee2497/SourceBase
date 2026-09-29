import { z } from "zod";
import type { Hono } from "hono";
import { validationErrorFromZod } from "../../lib/errors";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

const optionalPositiveInt = z.preprocess(
  value => (value === undefined ? undefined : Number(value)),
  z.number().int().positive().optional()
);

const querySchema = z.object({
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["Name", "CreatedOn", "UpdatedOn"]).optional()
});

const orderByColumns = {
  Name: "name",
  CreatedOn: "created_on",
  UpdatedOn: "updated_on"
} as const;

export function registerGetTodoListsRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 20;
    const orderBy = query.orderBy ?? "CreatedOn";
    const order = query.order ?? "Desc";
    const sqlOrder = order === "Asc" ? "ASC" : "DESC";
    const orderColumn = orderByColumns[orderBy];

    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM todo_lists WHERE user_id = ?`)
      .bind(currentUser.userId)
      .first<number>("value") ?? 0;

    const rows = await c.env.DB.prepare(
      `SELECT
          l.id,
          l.name,
          l.created_on,
          l.created_by,
          (SELECT COUNT(*) FROM todo_items t WHERE t.todo_list_id = l.id AND t.user_id = l.user_id) AS item_count,
          CASE WHEN l.id = u.default_todo_list_id THEN 1 ELSE 0 END AS is_default
       FROM todo_lists l
       INNER JOIN users u ON u.id = l.user_id
       WHERE l.user_id = ?
       ORDER BY l.${orderColumn} ${sqlOrder}
       LIMIT ? OFFSET ?`
    )
      .bind(currentUser.userId, limit, (page - 1) * limit)
      .all<{
        id: string;
        name: string;
        created_on: string | null;
        created_by: string | null;
        item_count: number;
        is_default: number;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        name: row.name,
        itemCount: Number(row.item_count ?? 0),
        createdOn: row.created_on,
        createdBy: row.created_by,
        isDefault: row.is_default === 1
      })),
      page,
      limit,
      total: Number(total)
    });
  });
}
