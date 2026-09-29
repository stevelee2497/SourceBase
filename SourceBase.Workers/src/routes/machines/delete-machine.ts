import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError } from "../../lib/errors";
import type { AppEnv } from "../../types";

export function registerDeleteMachineRoute(router: Hono<AppEnv>) {
  router.delete("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const machine = await c.env.DB.prepare(
      `SELECT id
       FROM machines
       WHERE id = ? AND user_id = ?`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{ id: string }>();

    if (!machine) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM machines WHERE id = ?`).bind(machine.id).run();
    return c.json({ id: machine.id });
  });
}
