import type { Hono } from "hono";
import type { AppEnv } from "../../types";

const iconGroups = ["Wallet", "Category", "General"] as const;

function parseIconGroup(value: string | null) {
  if (!value) return null;
  return iconGroups.find(group => group.toLowerCase() === value.toLowerCase()) ?? null;
}

export function registerGetIconsRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const parsedGroup = parseIconGroup(new URL(c.req.url).searchParams.get("group"));
    const rows = await c.env.DB.prepare(
      `SELECT id, value, name, group_name, sort_order, is_system
       FROM icons
       WHERE ? IS NULL OR group_name = ? OR group_name = 'General'
       ORDER BY sort_order ASC`
    )
      .bind(parsedGroup, parsedGroup)
      .all<{
        id: string;
        value: string;
        name: string;
        group_name: string;
        sort_order: number;
        is_system: number;
      }>();

    return c.json(rows.results.map(row => ({
      id: row.id,
      value: row.value,
      name: row.name,
      group: row.group_name,
      sortOrder: Number(row.sort_order),
      isSystem: row.is_system === 1
    })));
  });
}
