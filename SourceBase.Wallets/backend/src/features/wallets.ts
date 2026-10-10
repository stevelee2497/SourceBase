import { Hono } from "hono";
import { z } from "zod";
import { all, first, newId, now, run, stmt } from "../lib/db";
import { notFound } from "../lib/errors";
import { ok } from "../lib/json";
import type { AppEnv } from "../lib/types";
import { idParam, parseBody, required } from "../lib/validate";

type WalletRow = { id: string; name: string; initial_balance: number; currency: string; icon: string | null; balance: number };

const SELECT = `SELECT w.id, w.name, w.initial_balance, w.currency, w.icon,
  w.initial_balance + COALESCE((SELECT SUM(CASE t.type WHEN 'Income' THEN t.amount ELSE -t.amount END) FROM transactions t WHERE t.wallet_id = w.id), 0) AS balance
  FROM wallets w WHERE w.user_id = ?`;

const toWallet = (w: WalletRow) => ({ id: w.id, name: w.name, balance: w.balance, initialBalance: w.initial_balance, currency: w.currency, icon: w.icon });

const createSchema = z.object({ name: required("Name").max(100, "Name must be at most 100 characters."), initialBalance: z.number().default(0), currency: required("Currency"), icon: z.string().nullish() });
const updateSchema = z.object({ name: required("Name").max(100, "Name must be at most 100 characters.").nullish(), icon: z.string().nullish() });
const configSchema = z.object({ currency: required("Currency").max(10, "Currency must be at most 10 characters.") });

export const findOwned = async (db: D1Database, id: string, userId: string) => {
  const wallet = await first<WalletRow>(db, `${SELECT} AND w.id = ?`, userId, id);
  if (!wallet) throw notFound();
  return wallet;
};

export const wallets = new Hono<AppEnv>();

wallets.get("/", async (c) => {
  const rows = await all<WalletRow>(c.env.DB, `${SELECT} ORDER BY w.created_at, w.id`, c.get("userId"));
  return ok(c, { wallets: rows.map(toWallet), totalBalance: rows.reduce((sum, w) => sum + w.balance, 0) });
});

wallets.get("/:id", async (c) => ok(c, toWallet(await findOwned(c.env.DB, c.req.param("id"), c.get("userId")))));

wallets.post("/", async (c) => {
  const body = await parseBody(c, createSchema);
  const id = newId();
  await run(c.env.DB, "INSERT INTO wallets (id, user_id, name, initial_balance, currency, icon, created_at) VALUES (?, ?, ?, ?, ?, ?, ?)", id, c.get("userId"), body.name, body.initialBalance, body.currency, body.icon ?? null, now());
  return ok(c, { id });
});

wallets.patch("/:id", async (c) => {
  const id = idParam(c);
  const body = await parseBody(c, updateSchema);
  const wallet = await findOwned(c.env.DB, id, c.get("userId"));
  await run(c.env.DB, "UPDATE wallets SET name = ?, icon = ?, updated_at = ? WHERE id = ?", body.name ?? wallet.name, body.icon ?? wallet.icon, now(), id);
  return ok(c, { id });
});

wallets.put("/:id/config", async (c) => {
  const id = idParam(c);
  const body = await parseBody(c, configSchema);
  await findOwned(c.env.DB, id, c.get("userId"));
  await run(c.env.DB, "UPDATE wallets SET currency = ?, updated_at = ? WHERE id = ?", body.currency.toUpperCase(), now(), id);
  return ok(c, { id });
});

wallets.delete("/:id", async (c) => {
  const id = c.req.param("id");
  await findOwned(c.env.DB, id, c.get("userId"));
  const legs = "SELECT from_transaction_id FROM transfers WHERE from_wallet_id = ? OR to_wallet_id = ? UNION SELECT to_transaction_id FROM transfers WHERE from_wallet_id = ? OR to_wallet_id = ?";
  await c.env.DB.batch([stmt(c.env.DB, `DELETE FROM transactions WHERE id IN (${legs})`, id, id, id, id), stmt(c.env.DB, "DELETE FROM wallets WHERE id = ?", id)]);
  return ok(c, { success: true });
});

export const ownsWallet = async (db: D1Database, id: string, userId: string) => !!(await first(db, "SELECT 1 AS x FROM wallets WHERE id = ? AND user_id = ?", id, userId));
