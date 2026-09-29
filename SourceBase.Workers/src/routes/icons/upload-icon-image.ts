import { z } from "zod";
import type { Hono } from "hono";
import type { AppEnv } from "../../types";
import { readJson } from "../../lib/http";

const contentTypeMappings = {
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".gif": "image/gif",
  ".webp": "image/webp",
  ".svg": "image/svg+xml"
} as const;

const requestSchema = z.object({
  fileName: z.string().min(1).refine(value => getExtension(value) in contentTypeMappings, "Only jpg, png, gif, webp, and svg image files are allowed.")
});

export function registerUploadIconImageRoute(router: Hono<AppEnv>) {
  router.post("/upload-image", async c => {
    const request = await readJson(c, requestSchema);
    const ext = getExtension(request.fileName) as keyof typeof contentTypeMappings;
    const objectKey = `icons/${crypto.randomUUID()}${ext}`;
    const contentType = contentTypeMappings[ext];
    const origin = new URL(c.req.url).origin;
    const uploadBaseUrl = (c.env.ICON_UPLOAD_BASE_URL ?? origin).replace(/\/$/, "");
    const publicBaseUrl = (c.env.ICON_PUBLIC_BASE_URL ?? origin).replace(/\/$/, "");

    return c.json({
      uploadUrl: `${uploadBaseUrl}/uploads/${objectKey}?contentType=${encodeURIComponent(contentType)}&token=${crypto.randomUUID()}`,
      iconUrl: `${publicBaseUrl}/${objectKey}`,
      contentType
    });
  });
}

function getExtension(fileName: string) {
  const index = fileName.lastIndexOf(".");
  return index >= 0 ? fileName.slice(index).toLowerCase() : "";
}
