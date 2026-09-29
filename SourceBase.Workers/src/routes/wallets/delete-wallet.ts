import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";

export function registerDeleteWalletRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const wallet = await c.env.DB.prepare(
      `SELECT id, user_id
       FROM wallets
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{ id: string; user_id: string }>();

    if (!wallet || wallet.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM wallets WHERE id = ?`).bind(wallet.id).run();
    return c.json({ success: true });
  });
}
