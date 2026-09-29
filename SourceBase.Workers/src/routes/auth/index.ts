import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerConfirmEmailRoute } from "./confirm-email";
import { registerForgotPasswordRoute } from "./forgot-password";
import { registerGetUserInfoRoute } from "./get-user-info";
import { registerLoginRoute } from "./login";
import { registerLogoutRoute } from "./logout";
import { registerRefreshRoute } from "./refresh";
import { registerRegisterRoute } from "./register";
import { registerResendConfirmationEmailRoute } from "./resend-confirmation-email";
import { registerResetPasswordRoute } from "./reset-password";
import { registerUpdateUserInfoRoute } from "./update-user-info";

export function buildAuthRouter() {
  const router = new Hono<AppEnv>();
  registerConfirmEmailRoute(router);
  registerForgotPasswordRoute(router);
  registerGetUserInfoRoute(router);
  registerLoginRoute(router);
  registerLogoutRoute(router);
  registerRefreshRoute(router);
  registerRegisterRoute(router);
  registerResendConfirmationEmailRoute(router);
  registerResetPasswordRoute(router);
  registerUpdateUserInfoRoute(router);
  return router;
}
