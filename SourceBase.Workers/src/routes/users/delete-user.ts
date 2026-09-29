import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { roles } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerDeleteUserRoute(router: Hono<AppEnv>) {
  router.delete("/:id", requireRole(roles.admin), async c => {
    const id = z.string().uuid().parse(c.req.param("id"));
    const userId = await c.env.DB.prepare(`SELECT id FROM users WHERE id = ?`).bind(id).first<string>("id");
    if (!userId) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM users WHERE id = ?`).bind(id).run();
    return c.json({ success: true });
  });
}
