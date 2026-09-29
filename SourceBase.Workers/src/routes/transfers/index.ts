import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateTransferRoute } from "./create-transfer";
import { registerDeleteTransferRoute } from "./delete-transfer";
import { registerGetTransfersRoute } from "./get-transfers";

export function buildTransfersRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetTransfersRoute(router);
  registerCreateTransferRoute(router);
  registerDeleteTransferRoute(router);
  return router;
}
