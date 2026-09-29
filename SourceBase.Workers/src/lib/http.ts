import type { Context } from "hono";
import type { z } from "zod";
import { ValidationError, validationErrorFromZod } from "./errors";

export async function readJson<TSchema extends z.ZodTypeAny>(c: Context, schema: TSchema): Promise<z.infer<TSchema>> {
  let raw: unknown;
  try {
    raw = await c.req.json();
  } catch {
    throw new ValidationError({ body: ["The value is not valid."] });
  }

  const result = schema.safeParse(trimDeep(raw));
  if (!result.success) validationErrorFromZod(result);
  return result.data as z.infer<TSchema>;
}

export function trimDeep(value: unknown): unknown {
  if (typeof value === "string") return value.trim();
  if (Array.isArray(value)) return value.map(trimDeep);
  if (value && typeof value === "object") {
    return Object.fromEntries(Object.entries(value).map(([key, child]) => [key, trimDeep(child)]));
  }
  return value;
}
