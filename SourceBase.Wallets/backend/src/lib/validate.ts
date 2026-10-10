import { z } from "zod";
import { fieldError, validation, type FieldErrors } from "./errors";
import type { AppContext } from "./types";

export const NIL_ID = "00000000-0000-0000-0000-000000000000";

export const required = (label: string) => z.string({ error: `${label} is required.` }).min(1, `${label} is required.`);
export const isoDate = (label: string) => required(label).regex(/^\d{4}-\d{2}-\d{2}$/, `${label} must be yyyy-MM-dd.`).refine((v) => !Number.isNaN(Date.parse(v)), `${label} is invalid.`);
export const amount = z.number({ error: "Amount is required." }).gt(0, "Amount must be greater than 0.");
export const transactionType = z.enum(["Income", "Expense"], { error: "Type is required." });

const trim = (value: unknown): unknown => {
  if (typeof value === "string") return value.trim();
  if (Array.isArray(value)) return value.map(trim);
  if (value && typeof value === "object") return Object.fromEntries(Object.entries(value).map(([k, v]) => [k, trim(v)]));
  return value;
};

const toFieldErrors = (error: z.ZodError): FieldErrors => {
  const errors: FieldErrors = {};
  for (const issue of error.issues) (errors[String(issue.path[0] ?? "request")] ??= []).push(issue.message);
  return errors;
};

const parse = <T extends z.ZodType>(schema: T, data: unknown): z.output<T> => {
  const result = schema.safeParse(data);
  if (!result.success) throw validation(toFieldErrors(result.error));
  return result.data;
};

export const parseBody = async <T extends z.ZodType>(c: AppContext, schema: T): Promise<z.output<T>> => {
  const body = await c.req.json().catch(() => undefined);
  if (body === undefined) throw fieldError("request", "A valid JSON body is required.");
  return parse(schema, trim(body));
};

/** Query string to object. Keys in `arrayKeys` collect repeated values; empty values are ignored. */
export const parseQuery = <T extends z.ZodType>(c: AppContext, schema: T, arrayKeys: string[] = []): z.output<T> => {
  const params = new URL(c.req.url).searchParams;
  const data: Record<string, unknown> = {};
  for (const key of new Set(params.keys())) {
    const values = params.getAll(key).map((v) => v.trim()).filter(Boolean);
    if (values.length) data[key] = arrayKeys.includes(key) ? values : values[0];
  }
  return parse(schema, data);
};

/** Route id. The nil GUID is a validation error (400), anything else unknown is a 404 at lookup time. */
export const idParam = (c: AppContext) => {
  const id = c.req.param("id") ?? "";
  if (id === NIL_ID) throw fieldError("id", "Id is required.");
  return id;
};

export const paging = <const O extends readonly [string, ...string[]]>(orderBy: O, defaultOrderBy: O[number]) => z.object({
  page: z.coerce.number().int().min(1).default(1),
  limit: z.coerce.number().int().min(1).max(500).default(10),
  order: z.enum(["Asc", "Desc"]).default("Desc"),
  orderBy: z.enum(orderBy).default(defaultOrderBy),
});
