import { SELF } from "cloudflare:test";
import { describe, expect, it } from "vitest";
import { api, uniqueUser } from "../../helpers";

const pngBytes = () => new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 1, 2, 3, 4]);
const formWith = (fileName: string, bytes: Uint8Array = pngBytes(), type = "image/png") => {
  const form = new FormData();
  form.append("file", new File([bytes], fileName, { type }));
  return form;
};

describe("UploadIconImage", () => {
  it("ICONS-UPLOAD-001: without token returns 401", async () => {
    const res = await api(null, "POST", "/icons/upload", formWith("icon.png"));
    expect(res.status).toBe(401);
  });

  it("ICONS-UPLOAD-002: missing file returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons/upload", new FormData());
    expect(res.status).toBe(400);
  });

  it("ICONS-UPLOAD-003: unsupported extension returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons/upload", formWith("icon.bmp", pngBytes(), "image/bmp"));
    expect(res.status).toBe(400);
  });

  it("ICONS-UPLOAD-004: no extension returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons/upload", formWith("iconfile"));
    expect(res.status).toBe(400);
  });

  it.each(["jpg", "jpeg", "png", "gif", "webp", "svg"])("ICONS-UPLOAD-005 (extra): allowed extension .%s returns 200 and iconUrl", async (ext) => {
    const res = await api(uniqueUser(), "POST", "/icons/upload", formWith(`icon.${ext}`));
    expect(res.status).toBe(200);
    expect(res.body.iconUrl).toMatch(new RegExp(`/api/icons/files/[0-9a-f-]{36}\\.${ext}$`));
  });

  it("ICONS-UPLOAD-006 (extra): uploaded file can be fetched back without auth with the right content type", async () => {
    const upload = await api(uniqueUser(), "POST", "/icons/upload", formWith("icon.png"));
    expect(upload.status).toBe(200);

    const res = await SELF.fetch(upload.body.iconUrl);

    expect(res.status).toBe(200);
    expect(res.headers.get("Content-Type")).toBe("image/png");
    expect(new Uint8Array(await res.arrayBuffer())).toEqual(pngBytes());
  });

  it("ICONS-UPLOAD-007 (extra): file larger than 5 MB returns 400", async () => {
    const res = await api(uniqueUser(), "POST", "/icons/upload", formWith("big.png", new Uint8Array(5 * 1024 * 1024 + 1)));
    expect(res.status).toBe(400);
  });

  it("ICONS-UPLOAD-008 (extra): unknown file key returns 404", async () => {
    const res = await SELF.fetch(`https://example.com/api/icons/files/${crypto.randomUUID()}.png`);
    expect(res.status).toBe(404);
    await res.arrayBuffer();
  });
});
