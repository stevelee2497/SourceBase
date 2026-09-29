import { z } from "zod";
import type { Hono } from "hono";
import { requireRole } from "../../lib/auth-middleware";
import { nowIso, roles } from "../../lib/db";
import { BadRequestError, NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const requestSchema = z.object({
  name: z.string().min(1).max(256).nullable().optional(),
  description: z.string().min(1).max(500).nullable().optional()
});

export function registerUpdateRoleRoute(router: Hono<AppEnv>) {
  router.patch("/:id", requireRole(roles.admin), async c => {
    const currentUser = c.get("currentUser");
    const id = z.string().uuid().parse(c.req.param("id"));
    const request = await readJson(c, requestSchema);
    const role = await c.env.DB.prepare(`SELECT id, name, description FROM roles WHERE id = ?`)
      .bind(id)
      .first<{ id: string; name: string; description: string }>();

    if (!role) throw new NotFoundError();
    if (role.name === roles.admin) throw new BadRequestError("Admin role cannot be updated.");

    if (request.name !== null && request.name !== undefined) {
      const duplicateRole = await c.env.DB.prepare(`SELECT id FROM roles WHERE id != ? AND lower(name) = lower(?)`)
        .bind(id, request.name)
        .first<string>("id");
      if (duplicateRole) throw new BadRequestError("Role name is already taken.");
    }

    await c.env.DB.prepare(
      `UPDATE roles
       SET name = ?, description = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.name ?? role.name, request.description ?? role.description, nowIso(), currentUser.userName, id)
      .run();

    return c.json({ id });
  });
}
