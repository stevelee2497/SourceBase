import { defineConfig } from "vitest/config";
import { cloudflareTest, readD1Migrations } from "@cloudflare/vitest-plugin";
import { readFileSync } from "node:fs";

const jwks = readFileSync("./test/fixtures/test-jwks.json", "utf8");

export default defineConfig(async () => {
  const migrations = await readD1Migrations("./migrations");
  return {
    plugins: [cloudflareTest({ wrangler: { configPath: "./wrangler.jsonc" }, miniflare: { bindings: { TEST_MIGRATIONS: migrations, AUTH0_JWKS: jwks, AUTH0_DOMAIN: "test.auth0.local", AUTH0_AUDIENCE: "https://test-api" } } })],
    test: { setupFiles: ["./test/setup.ts"] },
  };
});
