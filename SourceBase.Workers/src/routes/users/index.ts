import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerConfirmUserEmailRoute } from "./confirm-user-email";
import { registerCreateUserRoute } from "./create-user";
import { registerDeleteUserRoute } from "./delete-user";
import { registerGetUsersRoute } from "./get-users";
import { registerResetUserPasswordRoute } from "./reset-user-password";
import { registerUpdateUserRoute } from "./update-user";

export function buildUsersRouter() {
  const router = new Hono<AppEnv>();
  registerGetUsersRoute(router);
  registerCreateUserRoute(router);
  registerUpdateUserRoute(router);
  registerDeleteUserRoute(router);
  registerConfirmUserEmailRoute(router);
  registerResetUserPasswordRoute(router);
  return router;
}
