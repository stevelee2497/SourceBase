import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";

export function registerGetUserInfoRoute(router: Hono<AppEnv>) {
  router.get("/info", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const user = await c.env.DB.prepare(
      `SELECT id, user_name, email, email_confirmed, first_name, last_name, phone_number, avatar_url, default_todo_list_id
       FROM users
       WHERE id = ?`
    )
      .bind(currentUser.userId)
      .first<{
        id: string;
        user_name: string;
        email: string;
        email_confirmed: number;
        first_name: string | null;
        last_name: string | null;
        phone_number: string | null;
        avatar_url: string | null;
        default_todo_list_id: string | null;
      }>();

    return c.json({
      id: user?.id,
      userName: user?.user_name,
      email: user?.email,
      emailConfirmed: user?.email_confirmed === 1,
      firstName: user?.first_name,
      lastName: user?.last_name,
      phoneNumber: user?.phone_number,
      avatarUrl: user?.avatar_url,
      defaultTodoListId: user?.default_todo_list_id,
      roles: currentUser.roles
    });
  });
}
