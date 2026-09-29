import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerConfigureWalletRoute } from "./configure-wallet";
import { registerCreateWalletRoute } from "./create-wallet";
import { registerDeleteWalletRoute } from "./delete-wallet";
import { registerGetWalletRoute } from "./get-wallet";
import { registerGetWalletsRoute } from "./get-wallets";
import { registerUpdateWalletRoute } from "./update-wallet";

export function buildWalletsRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetWalletsRoute(router);
  registerCreateWalletRoute(router);
  registerConfigureWalletRoute(router);
  registerGetWalletRoute(router);
  registerUpdateWalletRoute(router);
  registerDeleteWalletRoute(router);
  return router;
}
