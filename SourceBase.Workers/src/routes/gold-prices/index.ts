import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateGoldPriceRoute } from "./create-gold-price";
import { registerGetGoldPricesRoute } from "./get-gold-prices";

export function buildGoldPricesRouter() {
  const router = new Hono<AppEnv>();
  registerGetGoldPricesRoute(router);
  registerCreateGoldPriceRoute(router);
  return router;
}
