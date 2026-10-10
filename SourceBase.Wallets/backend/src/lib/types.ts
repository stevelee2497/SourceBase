import type { Context } from "hono";

export type AppEnv = { Bindings: Env; Variables: { userId: string; traceId: string } };
export type AppContext = Context<AppEnv>;
