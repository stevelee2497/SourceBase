import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { readJson } from "../../lib/http";
import { NotFoundError } from "../../lib/errors";
import { nowIso } from "../../lib/db";

const requestSchema = z.object({
  firstName: z.string().max(100).nullable().optional(),
  lastName: z.string().max(100).nullable().optional(),
  phoneNumber: z.string().max(20).nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  defaultTodoListId: z.string().uuid().nullable().optional()
});

export function registerUpdateUserInfoRoute(router: Hono<AppEnv>) {
  router.patch("/info", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const user = await c.env.DB.prepare(
      `SELECT id, first_name, last_name, phone_number, avatar_url, default_todo_list_id
       FROM users
       WHERE id = ?`
    )
      .bind(currentUser.userId)
      .first<{
        id: string;
        first_name: string | null;
        last_name: string | null;
        phone_number: string | null;
        avatar_url: string | null;
        default_todo_list_id: string | null;
      }>();

    if (!user) throw new NotFoundError();

    await c.env.DB.prepare(
      `UPDATE users
       SET first_name = ?, last_name = ?, phone_number = ?, avatar_url = ?, default_todo_list_id = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(
        request.firstName ?? user.first_name,
        request.lastName ?? user.last_name,
        request.phoneNumber ?? user.phone_number,
        request.avatarUrl ?? user.avatar_url,
        request.defaultTodoListId ?? user.default_todo_list_id,
        nowIso(),
        currentUser.userName,
        user.id
      )
      .run();

    return c.json({ id: user.id });
  });
}
