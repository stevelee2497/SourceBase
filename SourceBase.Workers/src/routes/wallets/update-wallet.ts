import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { NotFoundError } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const requestSchema = z.object({
  name: z.string().min(1).max(100).nullable().optional(),
  icon: z.string().nullable().optional()
});

export function registerUpdateWalletRoute(router: Hono<AppEnv>) {
  router.patch("/:id", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const wallet = await c.env.DB.prepare(
      `SELECT id, name, icon, user_id
       FROM wallets
       WHERE id = ?`
    )
      .bind(c.req.param("id"))
      .first<{
        id: string;
        name: string;
        icon: string | null;
        user_id: string;
      }>();

    if (!wallet || wallet.user_id !== currentUser.userId) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE wallets
       SET name = ?, icon = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(request.name ?? wallet.name, request.icon ?? wallet.icon, nowIso(), currentUser.userName, wallet.id)
      .run();

    return c.json({ id: wallet.id });
  });
}
