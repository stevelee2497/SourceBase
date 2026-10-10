import { Hono } from "hono";
import { z } from "zod";
import { all, first, newId, now, placeholders, run } from "../lib/db";
import { badRequest, fieldError, notFound } from "../lib/errors";
import { ok } from "../lib/json";
import type { AppEnv } from "../lib/types";
import { amount, idParam, isoDate, paging, parseBody, parseQuery, required, transactionType } from "../lib/validate";
import { ownsWallet } from "./wallets";

type TxRow = { id: string; amount: number; type: string; date: string; note: string | null; wallet_id: string; wallet_name: string; category_id: string | null; category_name: string | null; is_transfer: number };

const SELECT = `SELECT t.id, t.amount, t.type, t.date, t.note, t.wallet_id, w.name AS wallet_name, t.category_id, c.name AS category_name, t.is_transfer
  FROM transactions t JOIN wallets w ON w.id = t.wallet_id LEFT JOIN categories c ON c.id = t.category_id`;

const toTransaction = (t: TxRow) => ({ id: t.id, amount: t.amount, type: t.type, date: t.date, note: t.note, walletId: t.wallet_id, walletName: t.wallet_name, categoryId: t.category_id, categoryName: t.category_name, isTransfer: !!t.is_transfer });

const ORDER_COLUMNS = { Date: "t.date", Amount: "t.amount", Type: "t.type" } as const;

const listSchema = paging(["Date", "Amount", "Type"], "Date").extend({ walletIds: z.array(z.string()).optional(), categoryIds: z.array(z.string()).optional(), type: transactionType.optional(), dateFrom: isoDate("DateFrom").optional(), dateTo: isoDate("DateTo").optional(), exclude: z.enum(["Transfer"]).optional() });
const summarySchema = z.object({ walletIds: z.array(z.string()).optional(), dateFrom: isoDate("DateFrom").optional(), dateTo: isoDate("DateTo").optional() });
const createSchema = z.object({ walletId: required("WalletId"), amount, type: transactionType, date: isoDate("Date"), note: z.string().nullish(), categoryId: required("CategoryId") });
const updateSchema = z.object({ walletId: required("WalletId").nullish(), amount: amount.nullish(), type: transactionType.nullish(), date: isoDate("Date").nullish(), note: z.string().nullish(), categoryId: required("CategoryId").nullish() });

type Filter = { walletIds?: string[]; categoryIds?: string[]; type?: string; dateFrom?: string; dateTo?: string; exclude?: string };

/** WHERE clause for the current user plus the optional filters shared by the list and summary queries. */
const buildWhere = (userId: string, f: Filter, excludeTransfers = false) => {
  const clauses = ["t.user_id = ?"];
  const params: unknown[] = [userId];
  const add = (clause: string, ...values: unknown[]) => { clauses.push(clause); params.push(...values); };
  if (f.walletIds?.length) add(`t.wallet_id IN (${placeholders(f.walletIds)})`, ...f.walletIds);
  if (f.categoryIds?.length) add(`t.category_id IN (${placeholders(f.categoryIds)})`, ...f.categoryIds);
  if (f.type) add("t.type = ?", f.type);
  if (f.dateFrom) add("t.date >= ?", f.dateFrom);
  if (f.dateTo) add("t.date <= ?", f.dateTo);
  if (excludeTransfers || f.exclude === "Transfer") add("t.is_transfer = 0");
  return { where: clauses.join(" AND "), params };
};

const assertWalletOwned = async (db: D1Database, walletId: string, userId: string) => {
  if (!(await ownsWallet(db, walletId, userId))) throw fieldError("walletId", "Wallet not found.");
};

const assertCategoryUsable = async (db: D1Database, categoryId: string, userId: string) => {
  const category = await first(db, "SELECT 1 AS x FROM categories WHERE id = ? AND (is_system = 1 OR user_id = ?)", categoryId, userId);
  if (!category) throw fieldError("categoryId", "Category not found.");
};

const findOwned = async (db: D1Database, id: string, userId: string) => {
  const tx = await first<TxRow>(db, `${SELECT} WHERE t.id = ? AND t.user_id = ?`, id, userId);
  if (!tx) throw notFound();
  return tx;
};

export const transactions = new Hono<AppEnv>();

transactions.get("/", async (c) => {
  const q = parseQuery(c, listSchema, ["walletIds", "categoryIds"]);
  const { where, params } = buildWhere(c.get("userId"), q);
  const total = (await first<{ n: number }>(c.env.DB, `SELECT COUNT(*) AS n FROM transactions t WHERE ${where}`, ...params))!.n;
  const dir = q.order === "Asc" ? "ASC" : "DESC";
  const rows = await all<TxRow>(c.env.DB, `${SELECT} WHERE ${where} ORDER BY ${ORDER_COLUMNS[q.orderBy]} ${dir}, t.created_at ${dir}, t.id LIMIT ? OFFSET ?`, ...params, q.limit, (q.page - 1) * q.limit);
  return ok(c, { items: rows.map(toTransaction), page: q.page, limit: q.limit, total });
});

transactions.get("/summary", async (c) => {
  const q = parseQuery(c, summarySchema, ["walletIds"]);
  const { where, params } = buildWhere(c.get("userId"), q, true);
  const rows = await all<{ category_id: string | null; category_name: string | null; type: string; total: number }>(c.env.DB, `SELECT t.category_id, c.name AS category_name, t.type, SUM(t.amount) AS total FROM transactions t LEFT JOIN categories c ON c.id = t.category_id WHERE ${where} GROUP BY t.category_id, c.name, t.type`, ...params);
  const sum = (type: string) => rows.filter((r) => r.type === type).reduce((s, r) => s + r.total, 0);
  const [totalIncome, totalExpense] = [sum("Income"), sum("Expense")];
  return ok(c, { totalIncome, totalExpense, netBalance: totalIncome - totalExpense, byCategory: rows.map((r) => ({ categoryId: r.category_id, categoryName: r.category_name, type: r.type, total: r.total })) });
});

transactions.get("/:id", async (c) => ok(c, toTransaction(await findOwned(c.env.DB, c.req.param("id"), c.get("userId")))));

transactions.post("/", async (c) => {
  const body = await parseBody(c, createSchema);
  const userId = c.get("userId");
  await assertWalletOwned(c.env.DB, body.walletId, userId);
  await assertCategoryUsable(c.env.DB, body.categoryId, userId);
  const id = newId();
  await run(c.env.DB, "INSERT INTO transactions (id, user_id, wallet_id, category_id, amount, type, date, note, is_transfer, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, 0, ?)", id, userId, body.walletId, body.categoryId, body.amount, body.type, body.date, body.note ?? null, now());
  return ok(c, { id });
});

transactions.patch("/:id", async (c) => {
  const id = idParam(c);
  const body = await parseBody(c, updateSchema);
  const userId = c.get("userId");
  const tx = await findOwned(c.env.DB, id, userId);
  if (tx.is_transfer) throw badRequest("Transfer transactions cannot be edited directly. Delete the transfer instead.");
  if (body.walletId) await assertWalletOwned(c.env.DB, body.walletId, userId);
  if (body.categoryId) await assertCategoryUsable(c.env.DB, body.categoryId, userId);
  await run(c.env.DB, "UPDATE transactions SET wallet_id = ?, category_id = ?, amount = ?, type = ?, date = ?, note = ?, updated_at = ? WHERE id = ?", body.walletId ?? tx.wallet_id, body.categoryId ?? tx.category_id, body.amount ?? tx.amount, body.type ?? tx.type, body.date ?? tx.date, body.note ?? tx.note, now(), id);
  return ok(c, { id });
});

transactions.delete("/:id", async (c) => {
  const id = c.req.param("id");
  const tx = await findOwned(c.env.DB, id, c.get("userId"));
  if (tx.is_transfer) throw badRequest("Transfer transactions cannot be deleted directly. Delete the transfer instead.");
  await run(c.env.DB, "DELETE FROM transactions WHERE id = ?", id);
  return ok(c, { success: true });
});
