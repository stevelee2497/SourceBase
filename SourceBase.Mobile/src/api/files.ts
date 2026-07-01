import { api } from "./client";

// Two-step pre-signed avatar upload (mirrors the web app's avatar flow).
// Confirm the exact route + response field names against /swagger.
export const filesApi = {
  avatarUploadUrl: (fileName: string) =>
    api
      .post<{ uploadUrl: string; avatarUrl: string; contentType: string }>(
        "/files/avatar/upload-url",
        { fileName }
      )
      .then((r) => r.data),

  // PUT raw bytes to the pre-signed URL with bare fetch (no bearer header, no baseURL).
  putToSignedUrl: async (uploadUrl: string, blob: Blob, contentType: string) => {
    const res = await fetch(uploadUrl, {
      method: "PUT",
      headers: { "Content-Type": contentType },
      body: blob,
    });
    if (!res.ok) throw new Error(`Upload failed: ${res.status}`);
  },
};
