import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(100),
  initialBalance: z.number(),
  currency: z.string().min(1),
  icon: z.string().nullable().optional()
});

export function registerCreateWalletRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO wallets (
         id, name, initial_balance, currency, icon, user_id, created_on, created_by
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(id, request.name, request.initialBalance, request.currency, request.icon ?? null, currentUser.userId, nowIso(), currentUser.userName)
      .run();

    return c.json({ id });
  });
}
