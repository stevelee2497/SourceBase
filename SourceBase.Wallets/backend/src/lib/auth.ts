import { createMiddleware } from "hono/factory";
import { createLocalJWKSet, createRemoteJWKSet, jwtVerify } from "jose";
import { unauthorized } from "./errors";
import type { AppEnv } from "./types";

let remoteKeys: ReturnType<typeof createRemoteJWKSet> | undefined;

/** AUTH0_JWKS is only ever set by the test setup; in production keys come from the Auth0 tenant. */
const keysFor = (env: Env) => env.AUTH0_JWKS ? createLocalJWKSet(JSON.parse(env.AUTH0_JWKS)) : (remoteKeys ??= createRemoteJWKSet(new URL(`https://${env.AUTH0_DOMAIN}/.well-known/jwks.json`)));

export const requireAuth = createMiddleware<AppEnv>(async (c, next) => {
  const token = c.req.header("Authorization")?.match(/^Bearer (.+)$/i)?.[1];
  if (!token) throw unauthorized();
  const result = await jwtVerify(token, keysFor(c.env), { issuer: `https://${c.env.AUTH0_DOMAIN}/`, audience: c.env.AUTH0_AUDIENCE, algorithms: ["RS256"] }).catch(() => undefined);
  if (!result?.payload.sub) throw unauthorized();
  c.set("userId", result.payload.sub);
  await next();
});
