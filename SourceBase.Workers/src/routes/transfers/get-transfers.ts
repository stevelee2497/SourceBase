import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { validationErrorFromZod } from "../../lib/errors";

const querySchema = z.object({
  walletId: z.string().uuid().optional(),
  dateFrom: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  dateTo: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  page: z.coerce.number().int().positive().default(1),
  limit: z.coerce.number().int().positive().default(10),
  order: z.enum(["Asc", "Desc"]).default("Desc"),
  orderBy: z.enum(["Date", "Amount"]).default("Date")
});

const orderByColumns = {
  Date: "transfers.date",
  Amount: "transfers.amount"
} as const;

export function registerGetTransfersRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const url = new URL(c.req.url);
    const result = querySchema.safeParse({
      walletId: url.searchParams.get("walletId") ?? undefined,
      dateFrom: url.searchParams.get("dateFrom") ?? undefined,
      dateTo: url.searchParams.get("dateTo") ?? undefined,
      page: url.searchParams.get("page") ?? undefined,
      limit: url.searchParams.get("limit") ?? undefined,
      order: url.searchParams.get("order") ?? undefined,
      orderBy: url.searchParams.get("orderBy") ?? undefined
    });
    if (!result.success) validationErrorFromZod(result);
    const query = result.data!;

    const filters = [`transfers.user_id = ?`];
    const values: unknown[] = [currentUser.userId];

    if (query.walletId) {
      filters.push(`(transfers.from_wallet_id = ? OR transfers.to_wallet_id = ?)`);
      values.push(query.walletId, query.walletId);
    }

    if (query.dateFrom) {
      filters.push(`transfers.date >= ?`);
      values.push(query.dateFrom);
    }

    if (query.dateTo) {
      filters.push(`transfers.date <= ?`);
      values.push(query.dateTo);
    }

    const whereClause = filters.join(" AND ");
    const totalRow = await c.env.DB.prepare(`SELECT COUNT(*) AS total FROM transfers WHERE ${whereClause}`)
      .bind(...values)
      .first<{ total: number }>();
    const offset = (query.page - 1) * query.limit;
    const items = await c.env.DB.prepare(
      `SELECT
         transfers.id,
         transfers.from_wallet_id,
         from_wallet.name AS from_wallet_name,
         transfers.to_wallet_id,
         to_wallet.name AS to_wallet_name,
         transfers.amount,
         transfers.date,
         transfers.note
       FROM transfers
       INNER JOIN wallets AS from_wallet ON from_wallet.id = transfers.from_wallet_id
       INNER JOIN wallets AS to_wallet ON to_wallet.id = transfers.to_wallet_id
       WHERE ${whereClause}
       ORDER BY ${orderByColumns[query.orderBy]} ${query.order.toUpperCase()}, transfers.created_on ${query.order.toUpperCase()}, transfers.id ${query.order.toUpperCase()}
       LIMIT ? OFFSET ?`
    )
      .bind(...values, query.limit, offset)
      .all<{
        id: string;
        from_wallet_id: string;
        from_wallet_name: string;
        to_wallet_id: string;
        to_wallet_name: string;
        amount: number;
        date: string;
        note: string | null;
      }>();

    return c.json({
      items: items.results.map(item => ({
        id: item.id,
        fromWalletId: item.from_wallet_id,
        fromWalletName: item.from_wallet_name,
        toWalletId: item.to_wallet_id,
        toWalletName: item.to_wallet_name,
        amount: Number(item.amount),
        date: item.date,
        note: item.note
      })),
      page: query.page,
      limit: query.limit,
      total: Number(totalRow?.total ?? 0)
    });
  });
}
