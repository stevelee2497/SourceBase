import { z } from "zod";
import type { Hono } from "hono";
import { validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const optionalDateTimeSchema = z.string()
  .refine(value => !Number.isNaN(Date.parse(value)), "Invalid date.")
  .transform(value => new Date(value).toISOString())
  .optional();

const querySchema = z.object({
  from: optionalDateTimeSchema,
  to: optionalDateTimeSchema
});

export function registerGetHabitsRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const rows = await c.env.DB.prepare(
      `SELECT
          h.id,
          h.name,
          h.icon,
          h.is_system,
          (
            SELECT COUNT(*)
            FROM habit_logs hl
            WHERE hl.habit_id = h.id
              AND hl.user_id = ?
              AND (? IS NULL OR hl.occurred_at >= ?)
              AND (? IS NULL OR hl.occurred_at <= ?)
          ) AS log_count
       FROM habits h
       WHERE h.is_system = 1 OR h.user_id = ?
       ORDER BY log_count DESC, h.name ASC`
    )
      .bind(
        currentUser.userId,
        query.from ?? null,
        query.from ?? null,
        query.to ?? null,
        query.to ?? null,
        currentUser.userId
      )
      .all<{
        id: string;
        name: string;
        icon: string | null;
        is_system: number;
        log_count: number;
      }>();

    return c.json(rows.results.map(row => ({
      id: row.id,
      name: row.name,
      icon: row.icon,
      isSystem: row.is_system === 1,
      logCount: Number(row.log_count ?? 0)
    })));
  });
}
