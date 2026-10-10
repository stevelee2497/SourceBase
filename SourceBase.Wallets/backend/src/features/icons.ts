import { Hono } from "hono";
import { z } from "zod";
import { all, first, newId, now, run } from "../lib/db";
import { fieldError, forbidden, notFound } from "../lib/errors";
import { ok } from "../lib/json";
import type { AppContext, AppEnv } from "../lib/types";
import { idParam, parseBody, parseQuery, required } from "../lib/validate";

type IconRow = { id: string; user_id: string | null; value: string; name: string; grp: string; sort_order: number; is_system: number };

const toIcon = (i: IconRow) => ({ id: i.id, value: i.value, name: i.name, group: i.grp, sortOrder: i.sort_order, isSystem: !!i.is_system });

const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;
const CONTENT_TYPES: Record<string, string> = { ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".png": "image/png", ".gif": "image/gif", ".webp": "image/webp", ".svg": "image/svg+xml" };
const FILE_KEY = /^[0-9a-f-]{36}\.(jpg|jpeg|png|gif|webp|svg)$/;

const group = z.enum(["Wallet", "Category", "General"], { error: "Group is required." });
const nameSchema = required("Name").max(100, "Name must be at most 100 characters.");
const GROUPS = ["Wallet", "Category", "General"];
const listSchema = z.object({ group: z.string().optional() });
const createSchema = z.object({ value: required("Value"), name: nameSchema, group, sortOrder: z.number().int().default(0) });
const updateSchema = z.object({ value: required("Value").nullish(), name: nameSchema.nullish(), group: group.nullish(), sortOrder: z.number().int().nullish() });

/** Loads an icon the user may modify: 404 if missing, 403 if system, 404 if owned by someone else. */
const findEditable = async (db: D1Database, id: string, userId: string) => {
  const icon = await first<IconRow>(db, "SELECT * FROM icons WHERE id = ?", id);
  if (!icon) throw notFound();
  if (icon.is_system) throw forbidden();
  if (icon.user_id !== userId) throw notFound();
  return icon;
};

export const icons = new Hono<AppEnv>();

icons.get("/", async (c) => {
  const { group: g } = parseQuery(c, listSchema);
  const wanted = GROUPS.find((name) => name.toLowerCase() === g?.toLowerCase()) ?? null;
  const rows = await all<IconRow>(c.env.DB, "SELECT * FROM icons WHERE (is_system = 1 OR user_id = ?) AND (? IS NULL OR grp = ? OR grp = 'General') ORDER BY sort_order, name", c.get("userId"), wanted, wanted);
  return ok(c, rows.map(toIcon));
});

icons.post("/", async (c) => {
  const body = await parseBody(c, createSchema);
  const id = newId();
  await run(c.env.DB, "INSERT INTO icons (id, user_id, value, name, grp, sort_order, is_system, created_at) VALUES (?, ?, ?, ?, ?, ?, 0, ?)", id, c.get("userId"), body.value, body.name, body.group, body.sortOrder, now());
  return ok(c, { id });
});

icons.post("/upload", async (c) => {
  const form = await c.req.formData().catch(() => undefined);
  const file = form?.get("file");
  if (!(file instanceof File)) throw fieldError("file", "File is required.");
  const ext = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  if (!CONTENT_TYPES[ext]) throw fieldError("file", "Only jpg, png, gif, webp, and svg image files are allowed.");
  if (file.size > MAX_UPLOAD_BYTES) throw fieldError("file", "File must be 5 MB or smaller.");
  const key = `${newId()}${ext}`;
  await c.env.ICONS.put(`icons/${key}`, await file.arrayBuffer(), { httpMetadata: { contentType: CONTENT_TYPES[ext] } });
  return ok(c, { iconUrl: `${new URL(c.req.url).origin}/api/icons/files/${key}` });
});

icons.patch("/:id", async (c) => {
  const id = idParam(c);
  const body = await parseBody(c, updateSchema);
  const icon = await findEditable(c.env.DB, id, c.get("userId"));
  await run(c.env.DB, "UPDATE icons SET value = ?, name = ?, grp = ?, sort_order = ?, updated_at = ? WHERE id = ?", body.value ?? icon.value, body.name ?? icon.name, body.group ?? icon.grp, body.sortOrder ?? icon.sort_order, now(), id);
  return ok(c, { id });
});

icons.delete("/:id", async (c) => {
  const id = c.req.param("id");
  await findEditable(c.env.DB, id, c.get("userId"));
  await run(c.env.DB, "DELETE FROM icons WHERE id = ?", id);
  return ok(c, { success: true });
});

/** Public (no auth): icons are shown by plain <img> tags. Registered directly on the app in index.ts. */
export const serveIconFile = async (c: AppContext) => {
  const key = c.req.param("key") ?? "";
  const object = FILE_KEY.test(key) ? await c.env.ICONS.get(`icons/${key}`) : null;
  if (!object) throw notFound();
  return new Response(object.body, { headers: { "Content-Type": object.httpMetadata?.contentType ?? "application/octet-stream", "Cache-Control": "public, max-age=31536000, immutable", "X-Content-Type-Options": "nosniff", "Content-Security-Policy": "default-src 'none'; style-src 'unsafe-inline'; sandbox" } });
};
