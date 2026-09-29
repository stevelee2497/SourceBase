import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const isoDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => !Number.isNaN(Date.parse(`${value}T00:00:00.000Z`)), "Invalid date.");

const requestSchema = z.object({
  items: z.array(
    z.object({
      date: isoDateSchema,
      project: z.string().min(1),
      hours: z.number().gt(0).lte(8)
    })
  ).min(1)
});

export function registerCreateTimeSheetRoute(router: Hono<AppEnv>) {
  router.post("/", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const ids: string[] = [];

    for (const item of request.items) {
      const existing = await c.env.DB.prepare(
        `SELECT id
         FROM time_sheets
         WHERE user_id = ? AND date = ? AND project = ?`
      )
        .bind(currentUser.userId, item.date, item.project)
        .first<{ id: string }>();

      if (existing) {
        await c.env.DB.prepare(
          `UPDATE time_sheets
           SET hours = ?, updated_on = ?, updated_by = ?
           WHERE id = ?`
        )
          .bind(item.hours, nowIso(), currentUser.userName, existing.id)
          .run();
        ids.push(existing.id);
        continue;
      }

      const id = crypto.randomUUID();
      await c.env.DB.prepare(
        `INSERT INTO time_sheets (id, date, project, hours, user_id, created_on, created_by)
         VALUES (?, ?, ?, ?, ?, ?, ?)`
      )
        .bind(id, item.date, item.project, item.hours, currentUser.userId, nowIso(), currentUser.userName)
        .run();
      ids.push(id);
    }

    await c.env.DB.prepare(
      `INSERT INTO notifications (id, user_id, event, title, message, data, is_read, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(
        crypto.randomUUID(),
        currentUser.userId,
        "GlobalNotificationEvent",
        "Time Sheets Submitted",
        "Your time sheets have been submitted successfully.",
        "",
        0,
        nowIso(),
        currentUser.userName
      )
      .run();

    return c.json({ ids });
  });
}
