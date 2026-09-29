import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { validationErrorFromZod } from "../../lib/errors";

const querySchema = z.object({
  type: z.enum(["Income", "Expense"]).optional()
});

export function registerGetCategoriesRoute(router: Hono<AppEnv>) {
  router.get("/", async c => {
    const currentUser = c.get("currentUser");
    const result = querySchema.safeParse({
      type: new URL(c.req.url).searchParams.get("type") ?? undefined
    });
    if (!result.success) validationErrorFromZod(result);
    const query = result.data!;

    const filters = [`(is_system = 1 OR user_id = ?)`];
    const values: unknown[] = [currentUser.userId];

    if (query.type) {
      filters.push(`type = ?`);
      values.push(query.type);
    }

    const categories = await c.env.DB.prepare(
      `SELECT id, user_id, name, type, icon, is_system
       FROM categories
       WHERE ${filters.join(" AND ")}
       ORDER BY name ASC`
    )
      .bind(...values)
      .all<{
        id: string;
        user_id: string | null;
        name: string;
        type: "Income" | "Expense";
        icon: string | null;
        is_system: number;
      }>();

    return c.json(
      categories.results.map(category => ({
        id: category.id,
        userId: category.user_id,
        name: category.name,
        type: category.type,
        icon: category.icon,
        isSystem: category.is_system === 1
      }))
    );
  });
}
