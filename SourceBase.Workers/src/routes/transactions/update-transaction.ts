import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { BadRequestError, NotFoundError, ValidationError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  walletId: z.string().uuid().nullable().optional(),
  amount: z.number().gt(0).nullable().optional(),
  type: z.enum(["Income", "Expense"]).nullable().optional(),
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).nullable().optional(),
  note: z.string().nullable().optional(),
  categoryId: z.string().uuid().nullable().optional()
});

export function registerUpdateTransactionRoute(router: Hono<AppEnv>) {
  router.patch("/:id", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const transaction = await c.env.DB.prepare(
      `SELECT id, wallet_id, amount, type, date, note, category_id, user_id, is_transfer
       FROM transactions
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{
        id: string;
        wallet_id: string;
        amount: number;
        type: "Income" | "Expense";
        date: string;
        note: string | null;
        category_id: string | null;
        user_id: string;
        is_transfer: number;
      }>();

    if (!transaction || transaction.user_id !== currentUser.userId) throw new NotFoundError();
    if (transaction.is_transfer === 1)
      throw new ValidationError(undefined, "Transfer transactions cannot be edited directly. Delete the transfer instead.");

    if (request.walletId) {
      const wallet = await c.env.DB.prepare(`SELECT id FROM wallets WHERE id = ? AND user_id = ?`)
        .bind(request.walletId, currentUser.userId)
        .first<{ id: string }>();
      if (!wallet) throw new BadRequestError("Wallet not found.");
    }

    if (request.categoryId) {
      const category = await c.env.DB.prepare(`SELECT id FROM categories WHERE id = ? AND (is_system = 1 OR user_id = ?)`)
        .bind(request.categoryId, currentUser.userId)
        .first<{ id: string }>();
      if (!category) throw new BadRequestError("Category not found.");
    }

    await c.env.DB.prepare(
      `UPDATE transactions
       SET wallet_id = ?, amount = ?, type = ?, date = ?, note = ?, category_id = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(
        request.walletId ?? transaction.wallet_id,
        request.amount ?? transaction.amount,
        request.type ?? transaction.type,
        request.date ?? transaction.date,
        request.note ?? transaction.note,
        request.categoryId ?? transaction.category_id,
        nowIso(),
        currentUser.userName,
        transaction.id
      )
      .run();

    return c.json({ id: transaction.id });
  });
}
