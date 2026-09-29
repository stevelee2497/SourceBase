import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { validationErrorFromZod } from "../../lib/errors";

const querySchema = z.object({
  walletIds: z.array(z.string().uuid()).optional(),
  type: z.enum(["Income", "Expense"]).optional(),
  categoryIds: z.array(z.string().uuid()).optional(),
  dateFrom: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  dateTo: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  exclude: z.enum(["Transfer"]).optional(),
  page: z.coerce.number().int().positive().default(1),
  limit: z.coerce.number().int().positive().default(10),
  order: z.enum(["Asc", "Desc"]).default("Desc"),
  orderBy: z.enum(["Date", "Amount", "Type"]).default("Date")
});

const orderByColumns = {
  Date: "transactions.date",
  Amount: "transactions.amount",
  Type: "transactions.type"
} as const;

export function registerGetTransactionsRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const url = new URL(c.req.url);
    const result = querySchema.safeParse({
      walletIds: readArray(url.searchParams, "walletIds"),
      type: url.searchParams.get("type") ?? undefined,
      categoryIds: readArray(url.searchParams, "categoryIds"),
      dateFrom: url.searchParams.get("dateFrom") ?? undefined,
      dateTo: url.searchParams.get("dateTo") ?? undefined,
      exclude: url.searchParams.get("exclude") ?? undefined,
      page: url.searchParams.get("page") ?? undefined,
      limit: url.searchParams.get("limit") ?? undefined,
      order: url.searchParams.get("order") ?? undefined,
      orderBy: url.searchParams.get("orderBy") ?? undefined
    });
    if (!result.success) validationErrorFromZod(result);
    const query = result.data!;

    const filters = [`transactions.user_id = ?`];
    const values: unknown[] = [currentUser.userId];

    if (query.walletIds && query.walletIds.length > 0) {
      filters.push(`transactions.wallet_id IN (${query.walletIds.map(() => "?").join(", ")})`);
      values.push(...query.walletIds);
    }

    if (query.type) {
      filters.push(`transactions.type = ?`);
      values.push(query.type);
    }

    if (query.categoryIds && query.categoryIds.length > 0) {
      filters.push(`transactions.category_id IN (${query.categoryIds.map(() => "?").join(", ")})`);
      values.push(...query.categoryIds);
    }

    if (query.dateFrom) {
      filters.push(`transactions.date >= ?`);
      values.push(query.dateFrom);
    }

    if (query.dateTo) {
      filters.push(`transactions.date <= ?`);
      values.push(query.dateTo);
    }

    if (query.exclude === "Transfer") filters.push(`transactions.is_transfer = 0`);

    const whereClause = filters.join(" AND ");
    const totalRow = await c.env.DB.prepare(`SELECT COUNT(*) AS total FROM transactions WHERE ${whereClause}`)
      .bind(...values)
      .first<{ total: number }>();
    const offset = (query.page - 1) * query.limit;
    const items = await c.env.DB.prepare(
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
       WHERE ${whereClause}
       ORDER BY ${orderByColumns[query.orderBy]} ${query.order.toUpperCase()}, transactions.created_on ${query.order.toUpperCase()}, transactions.id ${query.order.toUpperCase()}
       LIMIT ? OFFSET ?`
    )
      .bind(...values, query.limit, offset)
      .all<{
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

    return c.json({
      items: items.results.map(item => ({
        id: item.id,
        amount: Number(item.amount),
        type: item.type,
        date: item.date,
        note: item.note,
        walletId: item.wallet_id,
        walletName: item.wallet_name,
        categoryId: item.category_id,
        categoryName: item.category_name,
        isTransfer: item.is_transfer === 1
      })),
      page: query.page,
      limit: query.limit,
      total: Number(totalRow?.total ?? 0)
    });
  });
}

function readArray(searchParams: URLSearchParams, key: string) {
  const raw = searchParams.getAll(key).flatMap(value => value.split(",")).map(value => value.trim()).filter(Boolean);
  return raw.length > 0 ? raw : undefined;
}
