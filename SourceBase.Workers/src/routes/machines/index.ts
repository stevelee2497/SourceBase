import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateMachineRoute } from "./create-machine";
import { registerDeleteMachineRoute } from "./delete-machine";
import { registerGetMachinesRoute } from "./get-machines";
import { registerRestartMachineRoute } from "./restart-machine";
import { registerShutdownMachineRoute } from "./shutdown-machine";
import { registerUpdateMachineRoute } from "./update-machine";

export function buildMachinesRouter() {
  const router = new Hono<AppEnv>();
  registerGetMachinesRoute(router);
  registerCreateMachineRoute(router);
  registerUpdateMachineRoute(router);
  registerDeleteMachineRoute(router);
  registerRestartMachineRoute(router);
  registerShutdownMachineRoute(router);
  return router;
}
