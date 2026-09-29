import type { Hono } from "hono";
import type { AppEnv } from "../../types";

export function registerGetWalletsRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const wallets = await c.env.DB.prepare(
      `SELECT
         wallets.id,
         wallets.name,
         wallets.initial_balance,
         wallets.currency,
         wallets.icon,
         wallets.initial_balance + COALESCE(SUM(CASE WHEN transactions.type = 'Income' THEN transactions.amount ELSE -transactions.amount END), 0) AS balance
       FROM wallets
       LEFT JOIN transactions ON transactions.wallet_id = wallets.id
       WHERE wallets.user_id = ?
       GROUP BY wallets.id, wallets.name, wallets.initial_balance, wallets.currency, wallets.icon
       ORDER BY wallets.created_on ASC, wallets.name ASC`
    )
      .bind(currentUser.userId)
      .all<{
        id: string;
        name: string;
        initial_balance: number;
        currency: string;
        icon: string | null;
        balance: number;
      }>();

    const items = wallets.results.map(wallet => ({
      id: wallet.id,
      name: wallet.name,
      balance: Number(wallet.balance),
      initialBalance: Number(wallet.initial_balance),
      currency: wallet.currency,
      icon: wallet.icon
    }));

    return c.json({
      wallets: items,
      totalBalance: items.reduce((total, wallet) => total + wallet.balance, 0)
    });
  });
}
