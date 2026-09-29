import { z } from "zod";
import type { Hono } from "hono";
import { validationErrorFromZod } from "../../lib/errors";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

const todoStatuses = ["Open", "Completed", "Archived"] as const;
const optionalPositiveInt = z.preprocess(
  value => (value === undefined ? undefined : Number(value)),
  z.number().int().positive().optional()
);
const isoDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => !Number.isNaN(Date.parse(`${value}T00:00:00.000Z`)), "Invalid date.");
const querySchema = z.object({
  status: z.enum(todoStatuses).optional(),
  date: isoDateSchema.optional(),
  todoListId: z.string().uuid().optional(),
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["Date", "Title", "Status", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy"]).optional()
});

const orderByColumns = {
  Date: "date",
  Title: "title",
  Status: "status",
  CreatedOn: "created_on",
  CreatedBy: "created_by",
  UpdatedOn: "updated_on",
  UpdatedBy: "updated_by"
} as const;

export function registerGetTodosRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 10;
    const where = ["user_id = ?"];
    const params: Array<string | number> = [currentUser.userId];

    if (query.status) {
      where.push("status = ?");
      params.push(query.status);
    }

    if (query.date) {
      where.push("date = ?");
      params.push(query.date);
    }

    if (query.todoListId) {
      where.push("todo_list_id = ?");
      params.push(query.todoListId);
    }

    const whereClause = where.join(" AND ");
    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM todo_items WHERE ${whereClause}`)
      .bind(...params)
      .first<number>("value") ?? 0;

    const orderClause = query.orderBy
      ? ` ORDER BY ${orderByColumns[query.orderBy]} ${query.order === "Desc" ? "DESC" : "ASC"}`
      : "";

    const rows = await c.env.DB.prepare(
      `SELECT id, user_id, date, title, status, todo_list_id, created_on, created_by, updated_on, updated_by
       FROM todo_items
       WHERE ${whereClause}${orderClause}
       LIMIT ? OFFSET ?`
    )
      .bind(...params, limit, (page - 1) * limit)
      .all<{
        id: string;
        user_id: string;
        date: string;
        title: string;
        status: string;
        todo_list_id: string | null;
        created_on: string | null;
        created_by: string | null;
        updated_on: string | null;
        updated_by: string | null;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        userId: row.user_id,
        date: row.date,
        title: row.title,
        status: row.status,
        todoListId: row.todo_list_id,
        createdOn: row.created_on,
        createdBy: row.created_by,
        updatedOn: row.updated_on,
        updatedBy: row.updated_by
      })),
      page,
      limit,
      total: Number(total)
    });
  });
}
