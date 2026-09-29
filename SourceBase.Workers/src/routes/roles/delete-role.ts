import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { roles } from "../../lib/db";
import { BadRequestError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerDeleteRoleRoute(router: Hono<AppEnv>) {
  router.delete("/:id", requireRole(roles.admin), async c => {
    const id = z.string().uuid().parse(c.req.param("id"));
    const role = await c.env.DB.prepare(`SELECT id, name FROM roles WHERE id = ?`).bind(id).first<{ id: string; name: string }>();
    if (!role || role.name === roles.admin) throw new BadRequestError("Role not found or cannot delete Admin role.");

    await c.env.DB.prepare(`DELETE FROM roles WHERE id = ?`).bind(id).run();
    return c.json({ success: true });
  });
}
