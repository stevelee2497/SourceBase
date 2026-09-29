import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerGenerateAvatarUploadUrlRoute } from "./generate-avatar-upload-url";

export function buildFilesRouter() {
  const router = new Hono<AppEnv>();
  registerGenerateAvatarUploadUrlRoute(router);
  return router;
}
