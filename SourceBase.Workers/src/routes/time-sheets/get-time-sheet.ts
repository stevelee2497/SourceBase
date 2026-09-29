import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { NotFoundError, validationErrorFromZod } from "../../lib/errors";

const emptyGuid = "00000000-0000-0000-0000-000000000000";

const paramsSchema = z.object({
  id: z.string().uuid().refine(value => value !== emptyGuid, "The value is not valid.")
});

export function registerGetTimeSheetRoute(router: Hono<AppEnv>) {
  router.get("/:id", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const parsedParams = paramsSchema.safeParse(c.req.param());
    if (!parsedParams.success) validationErrorFromZod(parsedParams);
    const params = parsedParams.data!;

    const timeSheet = await c.env.DB.prepare(
      `SELECT id, date, project, hours, created_on, created_by, updated_on, updated_by
       FROM time_sheets
       WHERE id = ? AND user_id = ?`
    )
      .bind(params.id, currentUser.userId)
      .first<{
        id: string;
        date: string;
        project: string;
        hours: number;
        created_on: string | null;
        created_by: string | null;
        updated_on: string | null;
        updated_by: string | null;
      }>();

    if (!timeSheet) throw new NotFoundError();

    return c.json({
      id: timeSheet.id,
      date: timeSheet.date,
      project: timeSheet.project,
      hours: Number(timeSheet.hours),
      createdOn: timeSheet.created_on,
      createdBy: timeSheet.created_by,
      updatedOn: timeSheet.updated_on,
      updatedBy: timeSheet.updated_by
    });
  });
}
