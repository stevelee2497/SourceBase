import { Hono } from "hono";
import type { AppEnv } from "./types";
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

  app.use("*", async (c, next) => {
    c.set("traceId", crypto.randomUUID());
    await next();
    c.header("x-trace-id", c.get("traceId"));
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
