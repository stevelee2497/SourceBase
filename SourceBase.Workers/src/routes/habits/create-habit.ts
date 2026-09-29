import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(100),
  icon: z.string().nullable().optional()
});

export function registerCreateHabitRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO habits (id, name, icon, user_id, is_system, created_on, created_by)
       VALUES (?, ?, ?, ?, 0, ?, ?)`
    )
      .bind(id, request.name, request.icon ?? null, currentUser.userId, nowIso(), currentUser.userName)
      .run();

    return c.json({ id });
  });
}
