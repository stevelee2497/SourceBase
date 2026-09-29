import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { nowIso, roles } from "../../lib/db";
import { BadRequestError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const requestSchema = z.object({
  name: z.string().min(1).max(256),
  description: z.string().min(1).max(500)
});

export function registerCreateRoleRoute(router: Hono<AppEnv>) {
  router.post("/", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const duplicateRole = await c.env.DB.prepare(`SELECT id FROM roles WHERE lower(name) = lower(?)`).bind(request.name).first<string>("id");
    if (duplicateRole) throw new BadRequestError("Role name is already taken.");

    const roleId = crypto.randomUUID();
    await c.env.DB.prepare(`INSERT INTO roles (id, name, description, created_on, created_by) VALUES (?, ?, ?, ?, ?)`)
      .bind(roleId, request.name, request.description, nowIso(), currentUser.userName)
      .run();

    return c.json({ id: roleId });
  });
}
