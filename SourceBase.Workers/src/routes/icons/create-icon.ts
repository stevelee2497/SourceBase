import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const iconGroups = ["Wallet", "Category", "General"] as const;

function parseIconGroup(value: string) {
  return iconGroups.find(group => group.toLowerCase() === value.toLowerCase());
}

const iconGroupSchema = z.string().transform((value, ctx) => {
  const group = parseIconGroup(value);
  if (!group) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid option." });
    return z.NEVER;
  }

  return group;
});

const requestSchema = z.object({
  value: z.string().min(1),
  name: z.string().min(1).max(100),
  group: iconGroupSchema,
  sortOrder: z.number().int()
});

export function registerCreateIconRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const id = crypto.randomUUID();

    await c.env.DB.prepare(
      `INSERT INTO icons (id, value, name, group_name, sort_order, is_system, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, 0, ?, ?)`
    )
      .bind(id, request.value, request.name, request.group, request.sortOrder, nowIso(), currentUser.userName)
      .run();

    return c.json({ id });
  });
}
