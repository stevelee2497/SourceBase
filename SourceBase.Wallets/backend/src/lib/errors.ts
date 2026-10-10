export type FieldErrors = Record<string, string[]>;

export class ApiError extends Error {
  constructor(public status: number, public code: string, message: string, public errors?: FieldErrors) { super(message); }
}

export const notFound = (message = "Resource not found.") => new ApiError(404, "NOT FOUND", message);
export const forbidden = (message = "You do not have permission to perform this action.") => new ApiError(403, "FORBIDDEN", message);
export const unauthorized = (message = "Unauthorized.") => new ApiError(401, "UNAUTHORIZE", message);
export const badRequest = (message: string) => new ApiError(400, "BAD REQUEST", message);
export const validation = (errors: FieldErrors) => new ApiError(400, "VALIDATION ERROR", "One or more validation errors occurred.", errors);
export const fieldError = (field: string, message: string) => validation({ [field]: [message] });
