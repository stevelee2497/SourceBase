import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const querySchema = z.object({
  page: z.coerce.number().int().positive().default(1),
  limit: z.coerce.number().int().positive().default(10),
  order: z.enum(["Asc", "Desc"]).default("Desc")
});

type AuditRow = {
  id: string;
  author: string | null;
  action: string;
  entity_type: string;
  entity_id: string;
  current: string | null;
  original: string | null;
  changes: string | null;
  action_on: string;
};

export function registerGetAuditsRoute(router: Hono<AppEnv>) {
  router.get("/audits", requireRole("Admin"), async c => {
    const parsed = querySchema.safeParse(Object.fromEntries(new URL(c.req.url).searchParams.entries()));
    if (!parsed.success) validationErrorFromZod(parsed);
    const query = parsed.data as z.infer<typeof querySchema>;
    const direction = query.order === "Asc" ? "ASC" : "DESC";
    const offset = (query.page - 1) * query.limit;

    const total = await c.env.DB.prepare(`SELECT COUNT(*) AS count FROM audit_histories`).first<number>("count");
    const rows = await c.env.DB.prepare(
      `SELECT id, author, action, entity_type, entity_id, current, original, changes, action_on
       FROM audit_histories
       ORDER BY action_on ${direction}, id ${direction}
       LIMIT ? OFFSET ?`
    )
      .bind(query.limit, offset)
      .all<AuditRow>();

    return c.json({
      items: rows.results.map(row => ({
        id: row.id,
        author: row.author,
        action: row.action,
        entityType: row.entity_type,
        entityId: row.entity_id,
        current: parseJsonOrNull(row.current),
        original: parseJsonOrNull(row.original),
        changes: parseJsonOrNull(row.changes),
        actionOn: row.action_on
      })),
      page: query.page,
      limit: query.limit,
      total: total ?? 0
    });
  });
}

function parseJsonOrNull(value: string | null) {
  if (!value) return null;
  return JSON.parse(value) as unknown;
}
