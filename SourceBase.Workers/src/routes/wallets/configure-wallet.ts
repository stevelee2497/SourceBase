import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  currency: z.string().min(1).max(10)
});

export function registerConfigureWalletRoute(router: Hono<AppEnv>) {
  router.put("/:id/config", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const wallet = await c.env.DB.prepare(
      `SELECT id, user_id
       FROM wallets
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{ id: string; user_id: string }>();

    if (!wallet || wallet.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE wallets
       SET currency = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.currency.toUpperCase(), nowIso(), currentUser.userName, wallet.id)
      .run();

    return c.json({ id: wallet.id });
  });
}
