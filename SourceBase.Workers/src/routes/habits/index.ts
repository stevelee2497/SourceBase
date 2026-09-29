import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateHabitRoute } from "./create-habit";
import { registerDeleteHabitRoute } from "./delete-habit";
import { registerGetHabitsRoute } from "./get-habits";
import { registerUpdateHabitRoute } from "./update-habit";

export function buildHabitsRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetHabitsRoute(router);
  registerCreateHabitRoute(router);
  registerUpdateHabitRoute(router);
  registerDeleteHabitRoute(router);
  return router;
}
