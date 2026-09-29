import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(100),
  type: z.enum(["Income", "Expense"]),
  icon: z.string().nullable().optional()
});

export function registerCreateCategoryRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO categories (
         id, name, type, icon, user_id, is_system, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(id, request.name, request.type, request.icon ?? null, currentUser.userId, 0, nowIso(), currentUser.userName)
      .run();

    return c.json({ id });
  });
}
