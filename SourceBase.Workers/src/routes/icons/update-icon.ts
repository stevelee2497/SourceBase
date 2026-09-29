import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { ForbiddenError, NotFoundError, validationErrorFromZod } from "../../lib/errors";
import { nowIso } from "../../lib/db";
import { readJson } from "../../lib/http";

const emptyGuid = "00000000-0000-0000-0000-000000000000";
const guidSchema = z.string().regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, "The value is not valid.");
const iconGroups = ["Wallet", "Category", "General"] as const;

function parseIconGroup(value: string) {
  return iconGroups.find(group => group.toLowerCase() === value.toLowerCase());
}

const paramsSchema = z.object({
  id: guidSchema.refine(value => value !== emptyGuid, "The value is not valid.")
});

const iconGroupSchema = z.string().transform((value, ctx) => {
  const group = parseIconGroup(value);
  if (!group) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid option." });
    return z.NEVER;
  }

  return group;
});

const requestSchema = z.object({
  value: z.string().min(1).nullable().optional(),
  name: z.string().min(1).max(100).nullable().optional(),
  group: z.union([iconGroupSchema, z.null()]).optional(),
  sortOrder: z.number().int().nullable().optional()
});

export function registerUpdateIconRoute(router: Hono<AppEnv>) {
  router.patch("/:id", async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const request = await readJson(c, requestSchema);
    const icon = await c.env.DB.prepare(
      `SELECT id, value, name, group_name, sort_order, is_system
       FROM icons
       WHERE id = ?`
    )
      .bind(params.id)
      .first<{
        id: string;
        value: string;
        name: string;
        group_name: string;
        sort_order: number;
        is_system: number;
      }>();

    if (!icon) throw new NotFoundError();
    if (icon.is_system === 1) throw new ForbiddenError();

    await c.env.DB.prepare(
      `UPDATE icons
       SET value = ?, name = ?, group_name = ?, sort_order = ?, updated_on = ?, updated_by = ?
       WHERE id = ?`
    )
      .bind(
        request.value ?? icon.value,
        request.name ?? icon.name,
        request.group ?? icon.group_name,
        request.sortOrder ?? icon.sort_order,
        nowIso(),
        currentUser.userName,
        icon.id
      )
      .run();

    return c.json({ id: icon.id });
  });
}
