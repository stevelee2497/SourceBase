import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { NotFoundError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerRestartMachineRoute(router: Hono<AppEnv>) {
  router.post("/:id/restart", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const machine = await c.env.DB.prepare(
      `SELECT id
       FROM machines
       WHERE id = ? AND user_id = ?`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{ id: string }>();

    if (!machine) throw new NotFoundError();

    await c.env.DB.prepare(
      `INSERT INTO machine_commands (id, user_id, machine_id, type, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?)`
    )
      .bind(crypto.randomUUID(), currentUser.userId, machine.id, "Restart", nowIso(), currentUser.userName)
      .run();

    return c.json({ message: "Restart command sent" });
  });
}
