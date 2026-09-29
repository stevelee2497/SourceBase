import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { BadRequestError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  fromWalletId: z.string().uuid(),
  toWalletId: z.string().uuid(),
  amount: z.number().gt(0),
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  note: z.string().nullable().optional()
}).superRefine((value, ctx) => {
  if (value.fromWalletId === value.toWalletId) {
    ctx.addIssue({
      code: "custom",
      path: ["toWalletId"],
      message: "Source and destination wallets must be different."
    });
  }
});

export function registerCreateTransferRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);

    const fromWallet = await c.env.DB.prepare(`SELECT id FROM wallets WHERE id = ? AND user_id = ?`)
      .bind(request.fromWalletId, currentUser.userId)
      .first<{ id: string }>();
    if (!fromWallet) throw new BadRequestError("Source wallet not found.");

    const toWallet = await c.env.DB.prepare(`SELECT id FROM wallets WHERE id = ? AND user_id = ?`)
      .bind(request.toWalletId, currentUser.userId)
      .first<{ id: string }>();
    if (!toWallet) throw new BadRequestError("Destination wallet not found.");

    const now = nowIso();
    const fromTransactionId = crypto.randomUUID();
    const toTransactionId = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO transactions (
         id, amount, type, date, note, wallet_id, category_id, user_id, is_transfer, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        fromTransactionId,
        request.amount,
        "Expense",
        request.date,
        request.note ?? null,
        request.fromWalletId,
        null,
        currentUser.userId,
        1,
        now,
        currentUser.userName
      )
      .run();

    await c.env.DB.prepare(
      `INSERT INTO transactions (
         id, amount, type, date, note, wallet_id, category_id, user_id, is_transfer, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        toTransactionId,
        request.amount,
        "Income",
        request.date,
        request.note ?? null,
        request.toWalletId,
        null,
        currentUser.userId,
        1,
        now,
        currentUser.userName
      )
      .run();

    const id = crypto.randomUUID();
    await c.env.DB.prepare(
      `INSERT INTO transfers (
         id, from_wallet_id, to_wallet_id, amount, date, note, from_transaction_id, to_transaction_id, user_id, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        id,
        request.fromWalletId,
        request.toWalletId,
        request.amount,
        request.date,
        request.note ?? null,
        fromTransactionId,
        toTransactionId,
        currentUser.userId,
        now,
        currentUser.userName
      )
      .run();

    return c.json({ id });
  });
}
