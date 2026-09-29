import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ValidationError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  walletId: z.string().uuid(),
  amount: z.number().gt(0),
  type: z.enum(["Income", "Expense"]),
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  note: z.string().nullable().optional(),
  categoryId: z.string().uuid()
});

export function registerCreateTransactionRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);

    const wallet = await c.env.DB.prepare(`SELECT id FROM wallets WHERE id = ? AND user_id = ?`)
      .bind(request.walletId, currentUser.userId)
      .first<{ id: string }>();
    const category = await c.env.DB.prepare(`SELECT id FROM categories WHERE id = ? AND (is_system = 1 OR user_id = ?)`)
      .bind(request.categoryId, currentUser.userId)
      .first<{ id: string }>();

    const errors: Record<string, string[]> = {};
    if (!wallet) errors.walletId = ["Wallet not found."];
    if (!category) errors.categoryId = ["Category not found."];
    if (Object.keys(errors).length > 0) throw new ValidationError(errors);

    const id = crypto.randomUUID();
    await c.env.DB.prepare(
      `INSERT INTO transactions (
         id, amount, type, date, note, wallet_id, category_id, user_id, is_transfer, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        id,
        request.amount,
        request.type,
        request.date,
        request.note ?? null,
        request.walletId,
        request.categoryId,
        currentUser.userId,
        0,
        nowIso(),
        currentUser.userName
      )
      .run();

    return c.json({ id });
  });
}
