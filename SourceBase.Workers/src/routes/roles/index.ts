import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateRoleRoute } from "./create-role";
import { registerDeleteRoleRoute } from "./delete-role";
import { registerGetRolesRoute } from "./get-roles";
import { registerUpdateRoleRoute } from "./update-role";

export function buildRolesRouter() {
  const router = new Hono<AppEnv>();
  registerGetRolesRoute(router);
  registerCreateRoleRoute(router);
  registerUpdateRoleRoute(router);
  registerDeleteRoleRoute(router);
  return router;
}
