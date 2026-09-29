import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const goldSources = ["SJC", "PNJ", "GiaVang", "KimKhanhVietHung", "NgocThinh"] as const;
const requestSchema = z.object({
  items: z.array(z.object({
    source: z.enum(goldSources),
    buyPrice: z.number().positive(),
    sellPrice: z.number().positive(),
    recordedAt: z.coerce.date()
  })).min(1)
});

export function registerCreateGoldPriceRoute(router: Hono<AppEnv>) {
  router.post("/", requireAuth, async c => {
    const request = await readJson(c, requestSchema);
    const ids: string[] = [];

    for (const item of request.items) {
      const recordedAt = floorToHour(item.recordedAt);
      const existing = await c.env.DB.prepare(
        `SELECT id
         FROM gold_prices
         WHERE source = ? AND recorded_at = ?`
      )
        .bind(item.source, recordedAt)
        .first<{ id: string }>();

      if (existing) {
        await c.env.DB.prepare(
          `UPDATE gold_prices
           SET buy_price = ?, sell_price = ?, updated_on = ?
           WHERE id = ?`
        )
          .bind(item.buyPrice, item.sellPrice, nowIso(), existing.id)
          .run();
        ids.push(existing.id);
        continue;
      }

      const id = crypto.randomUUID();
      await c.env.DB.prepare(
        `INSERT INTO gold_prices (
           id, source, buy_price, sell_price, recorded_at, created_on
         ) VALUES (?, ?, ?, ?, ?, ?)`
      )
        .bind(id, item.source, item.buyPrice, item.sellPrice, recordedAt, nowIso())
        .run();
      ids.push(id);
    }

    return c.json({ ids });
  });
}

function floorToHour(value: Date) {
  return new Date(Date.UTC(value.getUTCFullYear(), value.getUTCMonth(), value.getUTCDate(), value.getUTCHours(), 0, 0, 0)).toISOString();
}
