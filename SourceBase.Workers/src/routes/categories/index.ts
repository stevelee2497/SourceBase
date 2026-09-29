import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateCategoryRoute } from "./create-category";
import { registerDeleteCategoryRoute } from "./delete-category";
import { registerGetCategoriesRoute } from "./get-categories";
import { registerUpdateCategoryRoute } from "./update-category";

export function buildCategoriesRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetCategoriesRoute(router);
  registerCreateCategoryRoute(router);
  registerUpdateCategoryRoute(router);
  registerDeleteCategoryRoute(router);
  return router;
}
