import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateHabitLogsRoute } from "./create-habit-log";
import { registerDeleteHabitLogRoute } from "./delete-habit-log";
import { registerGetHabitLogsRoute } from "./get-habit-logs";

export function buildHabitLogsRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetHabitLogsRoute(router);
  registerCreateHabitLogsRoute(router);
  registerDeleteHabitLogRoute(router);
  return router;
}
