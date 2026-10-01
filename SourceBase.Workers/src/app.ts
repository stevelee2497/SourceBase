import { Hono } from "hono";
import { cors } from "hono/cors";
import type { AppEnv } from "./types";
import { ensureDatabaseReady } from "./lib/db";
import { writeApiError } from "./lib/errors";
import { buildAuthRouter } from "./routes/auth";
import { buildCategoriesRouter } from "./routes/categories";
import { buildDataRouter } from "./routes/data";
import { buildFilesRouter } from "./routes/files";
import { buildGoldPricesRouter } from "./routes/gold-prices";
import { buildHabitLogsRouter } from "./routes/habit-logs";
import { buildHabitsRouter } from "./routes/habits";
import { buildIconsRouter } from "./routes/icons";
import { buildMachinesRouter } from "./routes/machines";
import { buildNotificationsRouter } from "./routes/notifications";
import { buildRolesRouter } from "./routes/roles";
import { buildTimeSheetsRouter } from "./routes/time-sheets";
import { buildTodoListsRouter } from "./routes/todo-lists";
import { buildTodosRouter } from "./routes/todos";
import { buildTransactionsRouter } from "./routes/transactions";
import { buildTransfersRouter } from "./routes/transfers";
import { buildUsersRouter } from "./routes/users";
import { buildWalletsRouter } from "./routes/wallets";

export function createApp() {
  const app = new Hono<AppEnv>();

  const apiCors = cors({
    origin: (origin, c) => resolveAllowedOrigin(origin, c.env),
    allowMethods: ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    allowHeaders: ["Authorization", "Content-Type"],
    exposeHeaders: ["x-trace-id"],
    maxAge: 86400
  });

  app.use("*", async (c, next) => {
    c.set("traceId", crypto.randomUUID());
    await next();
    c.header("x-trace-id", c.get("traceId"));
  });
  app.use("/api/*", apiCors);
  app.options("/api/*", apiCors);
  app.use("/api/*", async (c, next) => {
    if (c.req.method === "OPTIONS") return next();
    await ensureDatabaseReady(c.env);
    await next();
  });

  app.onError((error, c) => writeApiError(c, error));

  app.get("/", c => c.json({ name: "SourceBase.Workers", status: "ok" }));

  const api = new Hono<AppEnv>();
  api.get("/health", c => c.json({ status: "ok" }));
  api.route("/auth", buildAuthRouter());
  api.route("/categories", buildCategoriesRouter());
  api.route("/data", buildDataRouter());
  api.route("/roles", buildRolesRouter());
  api.route("/users", buildUsersRouter());
  api.route("/wallets", buildWalletsRouter());
  api.route("/transactions", buildTransactionsRouter());
  api.route("/transfers", buildTransfersRouter());
  api.route("/notifications", buildNotificationsRouter());
  api.route("/machines", buildMachinesRouter());
  api.route("/gold-prices", buildGoldPricesRouter());
  api.route("/files", buildFilesRouter());
  api.route("/habits", buildHabitsRouter());
  api.route("/habit-logs", buildHabitLogsRouter());
  api.route("/icons", buildIconsRouter());
  api.route("/todo-lists", buildTodoListsRouter());
  api.route("/todos", buildTodosRouter());
  api.route("/time-sheets", buildTimeSheetsRouter());

  app.route("/api", api);
  return app;
}

function resolveAllowedOrigin(origin: string, bindings: AppEnv["Bindings"]) {
  if (!origin) return "*";

  const allowedOrigins = new Set(
    [
      ...splitOrigins(bindings.ALLOWED_ORIGINS),
      ...splitOrigins(bindings.WEB_URL),
      "http://localhost:3001",
      "https://localhost:3001",
      "http://127.0.0.1:3001",
      "https://127.0.0.1:3001"
    ].filter(Boolean)
  );

  return allowedOrigins.has(origin) ? origin : "null";
}

function splitOrigins(value?: string) {
  if (!value) return [];
  return value
    .split(",")
    .map(origin => origin.trim())
    .filter(Boolean)
    .map(origin => (origin.startsWith("http") ? origin : `https://${origin}`));
}
