import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";

export function registerDeleteTransferRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const transfer = await c.env.DB.prepare(
      `SELECT id, from_transaction_id, to_transaction_id, user_id
       FROM transfers
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{
        id: string;
        from_transaction_id: string;
        to_transaction_id: string;
        user_id: string;
      }>();

    if (!transfer || transfer.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(`DELETE FROM transactions WHERE id = ? OR id = ?`)
      .bind(transfer.from_transaction_id, transfer.to_transaction_id)
      .run();
    await c.env.DB.prepare(`DELETE FROM transfers WHERE id = ?`).bind(transfer.id).run();

    return c.json({ success: true });
  });
}
