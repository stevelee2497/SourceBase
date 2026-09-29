import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateTimeSheetRoute } from "./create-time-sheet";
import { registerDeleteTimeSheetRoute } from "./delete-time-sheet";
import { registerGetTimeSheetRoute } from "./get-time-sheet";
import { registerGetTimeSheetsRoute } from "./get-time-sheets";

export function buildTimeSheetsRouter() {
  const router = new Hono<AppEnv>();
  registerCreateTimeSheetRoute(router);
  registerDeleteTimeSheetRoute(router);
  registerGetTimeSheetRoute(router);
  registerGetTimeSheetsRoute(router);
  return router;
}
