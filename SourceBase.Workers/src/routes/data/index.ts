import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerGetAuditsRoute } from "./get-audits";
import { registerGetEnumsRoute } from "./get-enums";
import { registerGetRedisStatusRoute } from "./get-redis-status";
import { registerGetStatsRoute } from "./get-stats";

export function buildDataRouter() {
  const router = new Hono<AppEnv>();
  registerGetAuditsRoute(router);
  registerGetEnumsRoute(router);
  registerGetRedisStatusRoute(router);
  registerGetStatsRoute(router);
  return router;
}
