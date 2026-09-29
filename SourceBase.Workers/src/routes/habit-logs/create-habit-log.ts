import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const guidSchema = z.string().regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, "The value is not valid.");
const habitLogActions = ["HabitStarted", "Dismissed", "Snoozed", "SuppressedVideo"] as const;

function parseHabitLogAction(value: string) {
  return habitLogActions.find(action => action.toLowerCase() === value.toLowerCase());
}

const habitLogActionSchema = z.string().transform((value, ctx) => {
  const action = parseHabitLogAction(value);
  if (!action) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid option." });
    return z.NEVER;
  }

  return action;
});

const occurredAtSchema = z.string().transform((value, ctx) => {
  if (value.startsWith("0001-01-01T00:00:00")) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid date." });
    return z.NEVER;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid date." });
    return z.NEVER;
  }

  return parsed.toISOString();
});

const requestSchema = z.object({
  entries: z.array(z.object({
    habitId: guidSchema.refine(value => value !== emptyGuid, "The value is not valid.").nullable().optional(),
    habitName: z.string().nullable().optional(),
    action: habitLogActionSchema,
    occurredAt: occurredAtSchema
  })).min(1)
});

export function registerCreateHabitLogsRoute(router: Hono<AppEnv>) {
  router.post("/", async c => {
    const currentUser = c.get("currentUser");
    const request = await readJson(c, requestSchema);
    const ids: string[] = [];

    for (const entry of request.entries) {
      const id = crypto.randomUUID();
      ids.push(id);

      await c.env.DB.prepare(
        `INSERT INTO habit_logs (id, user_id, habit_id, habit_name, action, occurred_at, created_on, created_by)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
      )
        .bind(id, currentUser.userId, entry.habitId ?? null, entry.habitName ?? null, entry.action, entry.occurredAt, nowIso(), currentUser.userName)
        .run();
    }

    return c.json({ ids });
  });
}
