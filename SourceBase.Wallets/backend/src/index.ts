import { Hono } from "hono";
import { cors } from "hono/cors";
import { requireAuth } from "./lib/auth";
import { ApiError, notFound } from "./lib/errors";
import type { AppEnv } from "./lib/types";
import { categories } from "./features/categories";
import { icons, serveIconFile } from "./features/icons";
import { transactions } from "./features/transactions";
import { transfers } from "./features/transfers";
import { wallets } from "./features/wallets";

const app = new Hono<AppEnv>();

app.use("*", async (c, next) => { c.set("traceId", crypto.randomUUID()); await next(); });
app.use("*", (c, next) => cors({ origin: c.env.ALLOWED_ORIGINS.split(",").map((o) => o.trim()), allowHeaders: ["Authorization", "Content-Type"], allowMethods: ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"] })(c, next));

app.get("/api/icons/files/:key", serveIconFile);

const api = new Hono<AppEnv>();
api.use("*", requireAuth);
api.route("/wallets", wallets);
api.route("/transactions", transactions);
api.route("/transfers", transfers);
api.route("/categories", categories);
api.route("/icons", icons);
app.route("/api", api);

app.notFound((c) => c.json({ traceId: c.get("traceId"), code: "NOT FOUND", message: "Resource not found." }, 404));

app.onError((err, c) => {
  const traceId = c.get("traceId");
  if (err instanceof ApiError) return c.json({ traceId, code: err.code, message: err.message, errors: err.errors }, err.status as 400);
  console.error(traceId, err);
  return c.json({ traceId, code: "INTERNAL ERROR", message: "An unexpected error occurred." }, 500);
});

export default app;
