import { Hono } from "hono";
import { z } from "zod";
import { all, first, newId, now, run } from "../lib/db";
import { badRequest, forbidden, notFound } from "../lib/errors";
import { ok } from "../lib/json";
import type { AppEnv } from "../lib/types";
import { idParam, parseBody, parseQuery, required, transactionType } from "../lib/validate";

type CategoryRow = { id: string; user_id: string | null; name: string; type: string; icon: string | null; is_system: number };

const toCategory = (c: CategoryRow) => ({ id: c.id, userId: c.user_id, name: c.name, type: c.type, icon: c.icon, isSystem: !!c.is_system });

const nameSchema = required("Name").max(100, "Name must be at most 100 characters.");
const typeSchema = z.enum(["Income", "Expense"], { error: "Type is required." });
const createSchema = z.object({ name: nameSchema, type: typeSchema, icon: z.string().nullish() });
const updateSchema = z.object({ name: nameSchema.nullish(), icon: z.string().nullish() });
const listSchema = z.object({ type: transactionType.optional() });

/** Loads a category the user may modify: 404 if missing, 403 if system, 404 if owned by someone else. */
const findEditable = async (db: D1Database, id: string, userId: string) => {
  const category = await first<CategoryRow>(db, "SELECT * FROM categories WHERE id = ?", id);
  if (!category) throw notFound();
  if (category.is_system) throw forbidden();
  if (category.user_id !== userId) throw notFound();
  return category;
};

export const categories = new Hono<AppEnv>();

categories.get("/", async (c) => {
  const { type } = parseQuery(c, listSchema);
  const rows = await all<CategoryRow>(c.env.DB, "SELECT * FROM categories WHERE (is_system = 1 OR user_id = ?) AND (? IS NULL OR type = ?) ORDER BY name", c.get("userId"), type ?? null, type ?? null);
  return ok(c, rows.map(toCategory));
});

categories.post("/", async (c) => {
  const body = await parseBody(c, createSchema);
  const id = newId();
  await run(c.env.DB, "INSERT INTO categories (id, user_id, name, type, icon, is_system, created_at) VALUES (?, ?, ?, ?, ?, 0, ?)", id, c.get("userId"), body.name, body.type, body.icon ?? null, now());
  return ok(c, { id });
});

categories.patch("/:id", async (c) => {
  const id = idParam(c);
  const body = await parseBody(c, updateSchema);
  const category = await findEditable(c.env.DB, id, c.get("userId"));
  await run(c.env.DB, "UPDATE categories SET name = ?, icon = ?, updated_at = ? WHERE id = ?", body.name ?? category.name, body.icon ?? category.icon, now(), id);
  return ok(c, { id });
});

categories.delete("/:id", async (c) => {
  const id = c.req.param("id");
  await findEditable(c.env.DB, id, c.get("userId"));
  if (await first(c.env.DB, "SELECT 1 AS x FROM transactions WHERE category_id = ? LIMIT 1", id)) throw badRequest("Category is in use by transactions.");
  await run(c.env.DB, "DELETE FROM categories WHERE id = ?", id);
  return ok(c, { success: true });
});
