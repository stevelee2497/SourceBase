import { Hono } from "hono";
import { z } from "zod";
import { all, first, newId, now, stmt } from "../lib/db";
import { fieldError, notFound } from "../lib/errors";
import { ok } from "../lib/json";
import type { AppEnv } from "../lib/types";
import { amount, idParam, isoDate, paging, parseBody, parseQuery, required } from "../lib/validate";
import { ownsWallet } from "./wallets";

type TransferRow = { id: string; from_wallet_id: string; from_wallet_name: string; to_wallet_id: string; to_wallet_name: string; amount: number; date: string; note: string | null; from_transaction_id: string; to_transaction_id: string };

const SELECT = `SELECT t.id, t.from_wallet_id, f.name AS from_wallet_name, t.to_wallet_id, w.name AS to_wallet_name, t.amount, t.date, t.note, t.from_transaction_id, t.to_transaction_id
  FROM transfers t JOIN wallets f ON f.id = t.from_wallet_id JOIN wallets w ON w.id = t.to_wallet_id`;

const toTransfer = (t: TransferRow) => ({ id: t.id, fromWalletId: t.from_wallet_id, fromWalletName: t.from_wallet_name, toWalletId: t.to_wallet_id, toWalletName: t.to_wallet_name, amount: t.amount, date: t.date, note: t.note });

const ORDER_COLUMNS = { Date: "t.date", Amount: "t.amount" } as const;

const listSchema = paging(["Date", "Amount"], "Date").extend({ walletId: z.string().optional(), dateFrom: isoDate("DateFrom").optional(), dateTo: isoDate("DateTo").optional() });
const createSchema = z.object({ fromWalletId: required("FromWalletId"), toWalletId: required("ToWalletId"), amount, date: isoDate("Date"), note: z.string().nullish() });

export const transfers = new Hono<AppEnv>();

transfers.get("/", async (c) => {
  const q = parseQuery(c, listSchema);
  const clauses = ["t.user_id = ?"];
  const params: unknown[] = [c.get("userId")];
  if (q.walletId) { clauses.push("(t.from_wallet_id = ? OR t.to_wallet_id = ?)"); params.push(q.walletId, q.walletId); }
  if (q.dateFrom) { clauses.push("t.date >= ?"); params.push(q.dateFrom); }
  if (q.dateTo) { clauses.push("t.date <= ?"); params.push(q.dateTo); }
  const where = clauses.join(" AND ");
  const total = (await first<{ n: number }>(c.env.DB, `SELECT COUNT(*) AS n FROM transfers t WHERE ${where}`, ...params))!.n;
  const dir = q.order === "Asc" ? "ASC" : "DESC";
  const rows = await all<TransferRow>(c.env.DB, `${SELECT} WHERE ${where} ORDER BY ${ORDER_COLUMNS[q.orderBy]} ${dir}, t.created_at ${dir}, t.id LIMIT ? OFFSET ?`, ...params, q.limit, (q.page - 1) * q.limit);
  return ok(c, { items: rows.map(toTransfer), page: q.page, limit: q.limit, total });
});

transfers.post("/", async (c) => {
  const body = await parseBody(c, createSchema);
  const userId = c.get("userId");
  if (body.fromWalletId === body.toWalletId) throw fieldError("toWalletId", "Source and destination wallets must be different.");
  if (!(await ownsWallet(c.env.DB, body.fromWalletId, userId))) throw fieldError("fromWalletId", "Source wallet not found.");
  if (!(await ownsWallet(c.env.DB, body.toWalletId, userId))) throw fieldError("toWalletId", "Destination wallet not found.");
  const [id, fromTxId, toTxId, at] = [newId(), newId(), newId(), now()];
  const insertLeg = (txId: string, walletId: string, type: string) => stmt(c.env.DB, "INSERT INTO transactions (id, user_id, wallet_id, amount, type, date, note, is_transfer, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, 1, ?)", txId, userId, walletId, body.amount, type, body.date, body.note ?? null, at);
  await c.env.DB.batch([
    insertLeg(fromTxId, body.fromWalletId, "Expense"),
    insertLeg(toTxId, body.toWalletId, "Income"),
    stmt(c.env.DB, "INSERT INTO transfers (id, user_id, from_wallet_id, to_wallet_id, from_transaction_id, to_transaction_id, amount, date, note, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", id, userId, body.fromWalletId, body.toWalletId, fromTxId, toTxId, body.amount, body.date, body.note ?? null, at),
  ]);
  return ok(c, { id });
});

transfers.delete("/:id", async (c) => {
  const id = c.req.param("id");
  const transfer = await first<TransferRow>(c.env.DB, `${SELECT} WHERE t.id = ? AND t.user_id = ?`, id, c.get("userId"));
  if (!transfer) throw notFound();
  await c.env.DB.batch([stmt(c.env.DB, "DELETE FROM transactions WHERE id IN (?, ?)", transfer.from_transaction_id, transfer.to_transaction_id), stmt(c.env.DB, "DELETE FROM transfers WHERE id = ?", id)]);
  return ok(c, { success: true });
});
