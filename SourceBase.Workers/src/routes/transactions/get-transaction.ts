import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";

export function registerGetTransactionRoute(router: Hono<AppEnv>) {
  router.get("/:id", async c => {
    const currentUser = c.get("currentUser");
    const transaction = await c.env.DB.prepare(
      `SELECT
         transactions.id,
         transactions.amount,
         transactions.type,
         transactions.date,
         transactions.note,
         transactions.wallet_id,
         wallets.name AS wallet_name,
         transactions.category_id,
         categories.name AS category_name,
         transactions.is_transfer
       FROM transactions
       INNER JOIN wallets ON wallets.id = transactions.wallet_id
       LEFT JOIN categories ON categories.id = transactions.category_id
       WHERE transactions.id = ? AND transactions.user_id = ?`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{
        id: string;
        amount: number;
        type: "Income" | "Expense";
        date: string;
        note: string | null;
        wallet_id: string;
        wallet_name: string;
        category_id: string | null;
        category_name: string | null;
        is_transfer: number;
      }>();

    if (!transaction) throw new NotFoundError();

    return c.json({
      id: transaction.id,
      amount: Number(transaction.amount),
      type: transaction.type,
      date: transaction.date,
      note: transaction.note,
      walletId: transaction.wallet_id,
      walletName: transaction.wallet_name,
      categoryId: transaction.category_id,
      categoryName: transaction.category_name,
      isTransfer: transaction.is_transfer === 1
    });
  });
}
