declare namespace Cloudflare {
  interface Env {
    DB: D1Database;
    ICONS: R2Bucket;
    AUTH0_DOMAIN: string;
    AUTH0_AUDIENCE: string;
    ALLOWED_ORIGINS: string;
    AUTH0_JWKS?: string;
    TEST_MIGRATIONS: import("cloudflare:test").D1Migration[];
  }
}
interface Env extends Cloudflare.Env {}
