import type { Context } from "hono";
import { z, ZodError } from "zod";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly statusCode: number,
    public readonly errors?: Record<string, string[]>
  ) {
    super(message);
  }
}

export class ApiInternalError extends ApiError {
  constructor(message = "Something went wrong") {
    super(message, "GENERIC CODE", 500);
  }
}

export class NotFoundError extends ApiError {
  constructor(message = "Item Not Found") {
    super(message, "NOT FOUND", 404);
  }
}

export class UnauthorizedError extends ApiError {
  constructor(message = "UNAUTHORIZE") {
    super(message, "UNAUTHORIZE", 401);
  }
}

export class BadRequestError extends ApiError {
  constructor(message = "BAD REQUEST") {
    super(message, "BAD REQUEST", 400);
  }
}

export class ForbiddenError extends ApiError {
  constructor(message = "FORBIDDEN") {
    super(message, "FORBIDDEN", 403);
  }
}

export class ValidationError extends ApiError {
  constructor(errors?: Record<string, string[]>, message = "One or more validation errors occurred.") {
    super(message, "VALIDATION ERROR", 400, errors);
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (error instanceof ZodError) return new ValidationError(groupZodIssues(error));
  return new ApiInternalError(error instanceof Error ? error.message : "Something went wrong");
}

export function writeApiError(c: Context, error: unknown) {
  const apiError = toApiError(error);
  const status = apiError.statusCode as 400 | 401 | 403 | 404 | 500;
  return c.json(
    {
      traceId: c.get("traceId"),
      code: apiError.code,
      message: apiError.message,
      errors: apiError.errors
    },
    status
  );
}

export function validationErrorFromZod(result: z.ZodSafeParseError<unknown>) {
  throw new ValidationError(groupZodIssues(result.error));
}

function groupZodIssues(error: ZodError) {
  const grouped = new Map<string, string[]>();
  for (const issue of error.issues) {
    const path = issue.path.length > 0 ? String(issue.path[0]) : "body";
    const values = grouped.get(path) ?? [];
    values.push(issue.message);
    grouped.set(path, values);
  }

  return Object.fromEntries(grouped);
}
