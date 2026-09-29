import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

export function registerGetRedisStatusRoute(router: Hono<AppEnv>) {
  router.get("/redis-status", requireAuth, c => {
    const isOnline = String(c.env.REDIS_AVAILABLE ?? "false").trim().toLowerCase() === "true";
    return c.json({ isOnline });
  });
}
