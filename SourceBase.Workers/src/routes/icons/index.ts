import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateIconRoute } from "./create-icon";
import { registerDeleteIconRoute } from "./delete-icon";
import { registerGetIconsRoute } from "./get-icons";
import { registerUpdateIconRoute } from "./update-icon";
import { registerUploadIconImageRoute } from "./upload-icon-image";

export function buildIconsRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetIconsRoute(router);
  registerCreateIconRoute(router);
  registerUpdateIconRoute(router);
  registerDeleteIconRoute(router);
  registerUploadIconImageRoute(router);
  return router;
}
