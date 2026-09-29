import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const machineStatusSchema = z.enum(["Active", "Inactive", "Maintenance"]);
const requestSchema = z.object({
  alias: z.string().min(1).nullable().optional(),
  status: machineStatusSchema.nullable().optional()
});

export function registerUpdateMachineRoute(router: Hono<AppEnv>) {
  router.patch("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const machine = await c.env.DB.prepare(
      `SELECT id, alias, status
       FROM machines
       WHERE id = ? AND user_id = ?`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{ id: string; alias: string | null; status: string }>();

    if (!machine) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE machines
       SET alias = ?, status = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.alias ?? machine.alias, request.status ?? machine.status, nowIso(), currentUser.userName, machine.id)
      .run();

    return c.json({ id: machine.id });
  });
}
