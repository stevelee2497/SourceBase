import { z } from "zod";
import type { Hono } from "hono";
import { validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const habitLogActions = ["HabitStarted", "Dismissed", "Snoozed", "SuppressedVideo"] as const;
const orderByColumns = {
  OccurredAt: "occurred_at",
  Action: "action",
  HabitName: "habit_name",
  CreatedOn: "created_on"
} as const;

function parseHabitLogAction(value: string) {
  return habitLogActions.find(action => action.toLowerCase() === value.toLowerCase());
}

const optionalPositiveInt = z.preprocess(
  value => (value === undefined ? undefined : Number(value)),
  z.number().int().positive().optional()
);

const optionalDateTimeSchema = z.string()
  .refine(value => !Number.isNaN(Date.parse(value)), "Invalid date.")
  .transform(value => new Date(value).toISOString())
  .optional();

const habitLogActionSchema = z.string().transform((value, ctx) => {
  const action = parseHabitLogAction(value);
  if (!action) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid option." });
    return z.NEVER;
  }

  return action;
});

const querySchema = z.object({
  action: habitLogActionSchema.optional(),
  ignore: z.array(habitLogActionSchema).optional(),
  from: optionalDateTimeSchema,
  to: optionalDateTimeSchema,
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["OccurredAt", "Action", "HabitName", "CreatedOn"]).optional()
});

export function registerGetHabitLogsRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const searchParams = new URL(c.req.url).searchParams;
    const parsedQuery = querySchema.safeParse({
      action: searchParams.get("action") ?? searchParams.get("filter") ?? undefined,
      ignore: searchParams.getAll("ignore").flatMap(value => value.split(",")).map(value => value.trim()).filter(Boolean),
      from: searchParams.get("from") ?? undefined,
      to: searchParams.get("to") ?? undefined,
      page: searchParams.get("page") ?? undefined,
      limit: searchParams.get("limit") ?? undefined,
      order: searchParams.get("order") ?? undefined,
      orderBy: searchParams.get("orderBy") ?? undefined
    });
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 10;
    const where = ["user_id = ?"];
    const params: Array<string | number> = [currentUser.userId];

    if (query.action) {
      where.push("action = ?");
      params.push(query.action);
    }

    if (query.ignore && query.ignore.length > 0) {
      where.push(`action NOT IN (${query.ignore.map(() => "?").join(", ")})`);
      params.push(...query.ignore);
    }

    if (query.from) {
      where.push("occurred_at >= ?");
      params.push(query.from);
    }

    if (query.to) {
      where.push("occurred_at <= ?");
      params.push(query.to);
    }

    const whereClause = where.join(" AND ");
    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS value FROM habit_logs WHERE ${whereClause}`)
      .bind(...params)
      .first<number>("value") ?? 0;

    const orderClause = query.orderBy
      ? ` ORDER BY ${orderByColumns[query.orderBy]} ${query.order === "Desc" ? "DESC" : "ASC"}`
      : "";

    const rows = await c.env.DB.prepare(
      `SELECT id, habit_id, habit_name, action, occurred_at, created_on
       FROM habit_logs
       WHERE ${whereClause}${orderClause}
       LIMIT ? OFFSET ?`
    )
      .bind(...params, limit, (page - 1) * limit)
      .all<{
        id: string;
        habit_id: string | null;
        habit_name: string | null;
        action: string;
        occurred_at: string;
        created_on: string | null;
      }>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        habitId: row.habit_id,
        habitName: row.habit_name,
        action: row.action,
        occurredAt: row.occurred_at,
        createdOn: row.created_on
      })),
      page,
      limit,
      total: Number(total)
    });
  });
}
