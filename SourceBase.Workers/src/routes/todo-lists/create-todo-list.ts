import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(200)
});

export function registerCreateTodoListRoute(router: Hono<AppEnv>) {
  router.post("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO todo_lists (id, name, user_id, created_on, created_by)
       VALUES (?, ?, ?, ?, ?)`
    )
      .bind(id, request.name, currentUser.userId, nowIso(), currentUser.userName)
      .run();

    return c.json({ id });
  });
}
