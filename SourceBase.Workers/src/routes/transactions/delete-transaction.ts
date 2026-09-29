import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError, ValidationError } from "../../lib/errors";

export function registerDeleteTransactionRoute(router: Hono<AppEnv>) {
  router.delete("/:id", async c => {
    const currentUser = c.get("currentUser");
    const transaction = await c.env.DB.prepare(
      `SELECT id, user_id, is_transfer
       FROM transactions
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{
        id: string;
        user_id: string;
        is_transfer: number;
      }>();

    if (!transaction || transaction.user_id !== currentUser.userId) throw new NotFoundError();
    if (transaction.is_transfer === 1)
      throw new ValidationError(undefined, "Transfer transactions cannot be deleted directly. Delete the transfer instead.");

    await c.env.DB.prepare(`DELETE FROM transactions WHERE id = ?`).bind(transaction.id).run();
    return c.json({ success: true });
  });
}
