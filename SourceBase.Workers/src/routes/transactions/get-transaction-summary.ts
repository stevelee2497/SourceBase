import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { validationErrorFromZod } from "../../lib/errors";

const querySchema = z.object({
  walletId: z.string().uuid().optional(),
  dateFrom: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  dateTo: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional()
});

export function registerGetTransactionSummaryRoute(router: Hono<AppEnv>) {
  router.get("/summary", async c => {
    const currentUser = c.get("currentUser");
    const searchParams = new URL(c.req.url).searchParams;
    const result = querySchema.safeParse({
      walletId: searchParams.get("walletId") ?? undefined,
      dateFrom: searchParams.get("dateFrom") ?? undefined,
      dateTo: searchParams.get("dateTo") ?? undefined
    });
    if (!result.success) validationErrorFromZod(result);
    const query = result.data!;

    const filters = [`transactions.user_id = ?`, `transactions.is_transfer = 0`];
    const values: unknown[] = [currentUser.userId];

    if (query.walletId) {
      filters.push(`transactions.wallet_id = ?`);
      values.push(query.walletId);
    }

    if (query.dateFrom) {
      filters.push(`transactions.date >= ?`);
      values.push(query.dateFrom);
    }

    if (query.dateTo) {
      filters.push(`transactions.date <= ?`);
      values.push(query.dateTo);
    }

    const whereClause = filters.join(" AND ");
    const income = await c.env.DB.prepare(
      `SELECT COALESCE(SUM(amount), 0) AS total
       FROM transactions
       WHERE ${whereClause} AND type = 'Income'`
    )
      .bind(...values)
      .first<{ total: number }>();
    const expense = await c.env.DB.prepare(
      `SELECT COALESCE(SUM(amount), 0) AS total
       FROM transactions
       WHERE ${whereClause} AND type = 'Expense'`
    )
      .bind(...values)
      .first<{ total: number }>();
    const byCategory = await c.env.DB.prepare(
      `SELECT
         transactions.category_id,
         categories.name AS category_name,
         transactions.type,
         SUM(transactions.amount) AS total
       FROM transactions
       LEFT JOIN categories ON categories.id = transactions.category_id
       WHERE ${whereClause}
       GROUP BY transactions.category_id, categories.name, transactions.type
       ORDER BY categories.name ASC`
    )
      .bind(...values)
      .all<{
        category_id: string | null;
        category_name: string | null;
        type: "Income" | "Expense";
        total: number;
      }>();

    const totalIncome = Number(income?.total ?? 0);
    const totalExpense = Number(expense?.total ?? 0);

    return c.json({
      totalIncome,
      totalExpense,
      netBalance: totalIncome - totalExpense,
      byCategory: byCategory.results.map(item => ({
        categoryId: item.category_id,
        categoryName: item.category_name,
        type: item.type,
        total: Number(item.total)
      }))
    });
  });
}
