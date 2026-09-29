import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const machineStatusSchema = z.enum(["Active", "Inactive", "Maintenance"]);
const requestSchema = z.object({
  name: z.string().min(1),
  status: machineStatusSchema.optional()
});

export function registerCreateMachineRoute(router: Hono<AppEnv>) {
  router.post("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const now = nowIso();
    const machine = await c.env.DB.prepare(
      `SELECT id, status
       FROM machines
       WHERE user_id = ? AND lower(name) = lower(?)`
    )
      .bind(currentUser.userId, request.name)
      .first<{ id: string; status: string }>();

    if (!machine) {
      const id = crypto.randomUUID();
      await c.env.DB.prepare(
        `INSERT INTO machines (
           id, name, alias, status, last_reported_on, user_id, created_on, created_by
         ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
      )
        .bind(id, request.name, null, request.status ?? "Active", now, currentUser.userId, now, currentUser.userName)
        .run();
      return c.json({ id });
    }

    await c.env.DB.prepare(
      `UPDATE machines
       SET status = ?, last_reported_on = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.status ?? machine.status, now, now, currentUser.userName, machine.id)
      .run();

    return c.json({ id: machine.id });
  });
}
