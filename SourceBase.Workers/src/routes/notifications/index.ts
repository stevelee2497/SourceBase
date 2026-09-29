import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerClearAllNotificationsRoute } from "./clear-all-notifications";
import { registerGetNotificationsRoute } from "./get-notifications";
import { registerMarkAllNotificationsAsReadRoute } from "./mark-all-notifications-as-read";
import { registerMarkNotificationAsReadRoute } from "./mark-notification-as-read";

export function buildNotificationsRouter() {
  const router = new Hono<AppEnv>();
  registerGetNotificationsRoute(router);
  registerMarkNotificationAsReadRoute(router);
  registerMarkAllNotificationsAsReadRoute(router);
  registerClearAllNotificationsRoute(router);
  return router;
}
