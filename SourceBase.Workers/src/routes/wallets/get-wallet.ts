import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";

export function registerGetWalletRoute(router: Hono<AppEnv>) {
  router.get("/:id", async c => {
    const currentUser = c.get("currentUser");
    const wallet = await c.env.DB.prepare(
      `SELECT
         wallets.id,
         wallets.name,
         wallets.initial_balance,
         wallets.currency,
         wallets.icon,
         wallets.initial_balance + COALESCE(SUM(CASE WHEN transactions.type = 'Income' THEN transactions.amount ELSE -transactions.amount END), 0) AS balance
       FROM wallets
       LEFT JOIN transactions ON transactions.wallet_id = wallets.id
       WHERE wallets.id = ? AND wallets.user_id = ?
       GROUP BY wallets.id, wallets.name, wallets.initial_balance, wallets.currency, wallets.icon`
    )
      .bind(c.req.param("id"), currentUser.userId)
      .first<{
        id: string;
        name: string;
        initial_balance: number;
        currency: string;
        icon: string | null;
        balance: number;
      }>();

    if (!wallet) throw new NotFoundError();

    return c.json({
      id: wallet.id,
      name: wallet.name,
      balance: Number(wallet.balance),
      initialBalance: Number(wallet.initial_balance),
      currency: wallet.currency,
      icon: wallet.icon
    });
  });
}
