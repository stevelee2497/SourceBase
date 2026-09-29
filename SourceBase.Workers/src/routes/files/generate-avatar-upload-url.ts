import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { ApiInternalError, ValidationError } from "../../lib/errors";
import { readJson } from "../../lib/http";
import type { AppBindings, AppEnv } from "../../types";

const requestSchema = z.object({
  fileName: z.string().min(1)
});

const contentTypeMappings: Record<string, string> = {
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".gif": "image/gif",
  ".webp": "image/webp"
};

const encoder = new TextEncoder();

export function registerGenerateAvatarUploadUrlRoute(router: Hono<AppEnv>) {
  router.post("/avatar/upload-url", requireAuth, async c => {
    const request = await readJson(c, requestSchema);
    const extension = getExtension(request.fileName);
    const contentType = contentTypeMappings[extension];
    if (!contentType) throw new ValidationError({ fileName: ["Only jpg, png, gif, and webp image files are allowed."] });

    const config = getConfig(c.env);
    const objectKey = `avatars/${crypto.randomUUID()}${extension}`;
    const uploadUrl = await createPresignedUploadUrl(config, objectKey, contentType);
    const avatarUrl = `${config.publicUrl.replace(/\/+$/, "")}/${objectKey}`;

    return c.json({ uploadUrl, avatarUrl, contentType });
  });
}

function getExtension(fileName: string) {
  const dotIndex = fileName.lastIndexOf(".");
  return dotIndex >= 0 ? fileName.slice(dotIndex).toLowerCase() : "";
}

function getConfig(bindings: AppBindings) {
  if (!bindings.R2_ACCESS_KEY_ID || !bindings.R2_SECRET_ACCESS_KEY || !bindings.R2_BUCKET_NAME || !bindings.R2_ACCOUNT_ID || !bindings.R2_PUBLIC_URL)
    throw new ApiInternalError("R2 upload signing is not configured");

  return {
    accessKeyId: bindings.R2_ACCESS_KEY_ID,
    secretAccessKey: bindings.R2_SECRET_ACCESS_KEY,
    bucketName: bindings.R2_BUCKET_NAME,
    accountId: bindings.R2_ACCOUNT_ID,
    publicUrl: bindings.R2_PUBLIC_URL,
    expiresIn: getExpirySeconds(bindings.R2_UPLOAD_EXPIRES_IN)
  };
}

async function createPresignedUploadUrl(
  config: { accessKeyId: string; secretAccessKey: string; bucketName: string; accountId: string; publicUrl: string; expiresIn: number },
  objectKey: string,
  contentType: string
) {
  const host = `${config.accountId}.r2.cloudflarestorage.com`;
  const now = new Date();
  const amzDate = formatAmzDate(now);
  const dateStamp = amzDate.slice(0, 8);
  const credentialScope = `${dateStamp}/auto/s3/aws4_request`;
  const signedHeaders = "content-type;host";
  const canonicalUri = `/${encodeRfc3986(config.bucketName)}/${objectKey.split("/").map(encodeRfc3986).join("/")}`;
  const canonicalQuery = buildCanonicalQuery([
    ["X-Amz-Algorithm", "AWS4-HMAC-SHA256"],
    ["X-Amz-Credential", `${config.accessKeyId}/${credentialScope}`],
    ["X-Amz-Date", amzDate],
    ["X-Amz-Expires", String(config.expiresIn)],
    ["X-Amz-SignedHeaders", signedHeaders]
  ]);
  const canonicalHeaders = `content-type:${contentType}\nhost:${host}\n`;
  const canonicalRequest = ["PUT", canonicalUri, canonicalQuery, canonicalHeaders, signedHeaders, "UNSIGNED-PAYLOAD"].join("\n");
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256Hex(canonicalRequest)].join("\n");
  const signature = toHex(await signString(await getSignatureKey(config.secretAccessKey, dateStamp, "auto", "s3"), stringToSign));
  return `https://${host}${canonicalUri}?${canonicalQuery}&X-Amz-Signature=${signature}`;
}

function getExpirySeconds(raw: string | undefined) {
  const parsed = Number(raw ?? "600");
  if (!Number.isFinite(parsed)) return 600;
  return Math.min(604800, Math.max(1, Math.trunc(parsed)));
}

function formatAmzDate(value: Date) {
  const iso = value.toISOString();
  return `${iso.slice(0, 4)}${iso.slice(5, 7)}${iso.slice(8, 10)}T${iso.slice(11, 13)}${iso.slice(14, 16)}${iso.slice(17, 19)}Z`;
}

function encodeRfc3986(value: string) {
  return encodeURIComponent(value).replace(/[!'()*]/g, char => `%${char.charCodeAt(0).toString(16).toUpperCase()}`);
}

function buildCanonicalQuery(entries: Array<[string, string]>) {
  return entries
    .map(([key, value]) => [encodeRfc3986(key), encodeRfc3986(value)] as const)
    .sort(([leftKey, leftValue], [rightKey, rightValue]) => leftKey.localeCompare(rightKey) || leftValue.localeCompare(rightValue))
    .map(([key, value]) => `${key}=${value}`)
    .join("&");
}

async function sha256Hex(value: string) {
  const digest = await crypto.subtle.digest("SHA-256", encoder.encode(value));
  return toHex(new Uint8Array(digest));
}

async function getSignatureKey(secretAccessKey: string, dateStamp: string, region: string, service: string) {
  const kDate = await signString(encoder.encode(`AWS4${secretAccessKey}`), dateStamp);
  const kRegion = await signString(kDate, region);
  const kService = await signString(kRegion, service);
  return signString(kService, "aws4_request");
}

async function signString(key: Uint8Array, value: string) {
  const rawKey = Uint8Array.from(key).buffer;
  const cryptoKey = await crypto.subtle.importKey("raw", rawKey, { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  const signature = await crypto.subtle.sign("HMAC", cryptoKey, encoder.encode(value));
  return new Uint8Array(signature);
}

function toHex(bytes: Uint8Array) {
  return [...bytes].map(value => value.toString(16).padStart(2, "0")).join("");
}
