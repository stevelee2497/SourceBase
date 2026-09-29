import { z } from "zod";
import type { Hono } from "hono";
import { validationErrorFromZod } from "../../lib/errors";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

const optionalPositiveInt = z.preprocess(
  value => (value === undefined ? undefined : Number(value)),
  z.number().int().positive().optional()
);
const isoDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => !Number.isNaN(Date.parse(`${value}T00:00:00.000Z`)), "Invalid date.");
const querySchema = z.object({
  from: isoDateSchema.optional(),
  to: isoDateSchema.optional(),
  year: optionalPositiveInt,
  month: z.preprocess(value => (value === undefined ? undefined : Number(value)), z.number().int().min(1).max(12).optional()),
  date: isoDateSchema.optional(),
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["Date", "Project", "Hours", "CreatedOn", "UpdatedOn"]).optional()
});

const orderByColumns = {
  Date: "date",
  Project: "project",
  Hours: "hours",
  CreatedOn: "created_on",
  UpdatedOn: "updated_on"
} as const;

export function registerGetTimeSheetsRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 10;
    const from = query.from ?? query.date ?? buildMonthBoundary(query.year, query.month, "start");
    const to = query.to ?? query.date ?? buildMonthBoundary(query.year, query.month, "end");
    const where = ["user_id = ?"];
    const params: Array<string | number> = [currentUser.userId];

    if (from) {
      where.push("date >= ?");
      params.push(from);
    }

    if (to) {
      where.push("date <= ?");
      params.push(to);
    }

    const whereClause = where.join(" AND ");
    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM time_sheets WHERE ${whereClause}`)
      .bind(...params)
      .first<number>("value") ?? 0;

    const orderClause = query.orderBy
      ? ` ORDER BY ${orderByColumns[query.orderBy]} ${query.order === "Desc" ? "DESC" : "ASC"}`
      : "";

    const rows = await c.env.DB.prepare(
      `SELECT id, date, project, hours, created_on, created_by, updated_on, updated_by
       FROM time_sheets
       WHERE ${whereClause}${orderClause}
       LIMIT ? OFFSET ?`
    )
      .bind(...params, limit, (page - 1) * limit)
      .all<{
        id: string;
        date: string;
        project: string;
        hours: number;
        created_on: string | null;
        created_by: string | null;
        updated_on: string | null;
        updated_by: string | null;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        date: row.date,
        project: row.project,
        hours: Number(row.hours),
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

function buildMonthBoundary(year?: number, month?: number, edge?: "start" | "end") {
  if (!year || !month) return undefined;
  const date = edge === "start"
    ? new Date(Date.UTC(year, month - 1, 1))
    : new Date(Date.UTC(year, month, 0));
  return date.toISOString().slice(0, 10);
}
