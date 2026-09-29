import { z } from "zod";
import type { Hono } from "hono";
import { readJson } from "../../lib/http";
import type { AppEnv } from "../../types";

const enumNames = ["RolesOrder", "TodoItemStatus", "Roles"] as const;
const requestSchema = z.object({
  enums: z.array(z.enum(enumNames)).min(1)
});

const rolesOrderDefinitions = ["Name", "Description", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy"]
  .map(value => ({ name: value, description: value }));
const todoItemStatusDefinitions = ["Open", "Completed", "Archived"].map(value => ({ name: value, description: value }));

export function registerGetEnumsRoute(router: Hono<AppEnv>) {
  router.post("/enums", async c => {
    const request = await readJson(c, requestSchema);
    const data = Object.fromEntries(
      await Promise.all(
        request.enums.map(async enumName => {
          if (enumName === "RolesOrder") return [enumName, rolesOrderDefinitions] as const;
          if (enumName === "TodoItemStatus") return [enumName, todoItemStatusDefinitions] as const;

          const roles = await c.env.DB.prepare(`SELECT name, description FROM roles WHERE name IS NOT NULL ORDER BY name`)
            .all<{ name: string; description: string }>();
          return [enumName, roles.results.map(role => ({ name: role.name, description: role.description }))] as const;
        })
      )
    );

    return c.json({ data });
  });
}
