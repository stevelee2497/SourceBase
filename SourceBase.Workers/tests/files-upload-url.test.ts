import { beforeEach, describe, expect, it } from "vitest";
import { authHeaders, createTestContext, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers files routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("requires auth for avatar upload url generation", async () => {
    const response = await send(context, "http://localhost/api/files/avatar/upload-url", {
      method: "POST",
      body: JSON.stringify({ fileName: "avatar.png" })
    });
    expect(response.status).toBe(401);
  });

  it("returns a signed avatar upload contract", async () => {
    const response = await send(context, "http://localhost/api/files/avatar/upload-url", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ fileName: "avatar.PNG" })
    });

    expect(response.status).toBe(200);
    const body = await response.json() as { uploadUrl: string; avatarUrl: string; contentType: string };
    const uploadUrl = new URL(body.uploadUrl);

    expect(body.contentType).toBe("image/png");
    expect(body.avatarUrl).toMatch(/^https:\/\/cdn\.sourcebase\.test\/avatars\/.+\.png$/);
    expect(uploadUrl.host).toBe("account-123.r2.cloudflarestorage.com");
    expect(uploadUrl.pathname).toMatch(/^\/sourcebase\/avatars\/.+\.png$/);
    expect(uploadUrl.searchParams.get("X-Amz-Algorithm")).toBe("AWS4-HMAC-SHA256");
    expect(uploadUrl.searchParams.get("X-Amz-SignedHeaders")).toBe("content-type;host");
    expect(uploadUrl.searchParams.get("X-Amz-Signature")).toBeTruthy();
  });

  it("rejects unsupported avatar file extensions", async () => {
    const response = await send(context, "http://localhost/api/files/avatar/upload-url", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({ fileName: "avatar.bmp" })
    });

    expect(response.status).toBe(400);
    const body = await response.json() as { errors?: Record<string, string[]> };
    expect(body.errors?.fileName).toContain("Only jpg, png, gif, and webp image files are allowed.");
  });
});
