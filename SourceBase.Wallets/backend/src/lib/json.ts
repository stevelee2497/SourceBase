import type { AppContext } from "./types";

const strip = (value: unknown): unknown => {
  if (Array.isArray(value)) return value.map(strip);
  if (value === null || typeof value !== "object") return value;
  return Object.fromEntries(Object.entries(value).filter(([, v]) => v !== null && v !== undefined).map(([k, v]) => [k, strip(v)]));
};

/** JSON response with null fields omitted (matches the .NET WhenWritingNull behaviour). */
export const ok = (c: AppContext, data: unknown) => c.json(strip(data) as object);
