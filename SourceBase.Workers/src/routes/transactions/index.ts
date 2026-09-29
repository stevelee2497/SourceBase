import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { requireAuth } from "../../lib/auth-middleware";
import { registerCreateTransactionRoute } from "./create-transaction";
import { registerDeleteTransactionRoute } from "./delete-transaction";
import { registerGetTransactionRoute } from "./get-transaction";
import { registerGetTransactionSummaryRoute } from "./get-transaction-summary";
import { registerGetTransactionsRoute } from "./get-transactions";
import { registerUpdateTransactionRoute } from "./update-transaction";

export function buildTransactionsRouter() {
  const router = new Hono<AppEnv>();
  router.use("*", requireAuth);
  registerGetTransactionSummaryRoute(router);
  registerGetTransactionsRoute(router);
  registerCreateTransactionRoute(router);
  registerGetTransactionRoute(router);
  registerUpdateTransactionRoute(router);
  registerDeleteTransactionRoute(router);
  return router;
}
