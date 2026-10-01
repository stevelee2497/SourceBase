export interface SqlRunMeta {
  changes?: number;
  last_row_id?: number | string;
}

export interface SqlRunResult {
  success: boolean;
  meta?: SqlRunMeta;
}

export interface SqlAllResult<T> {
  success: boolean;
  results: T[];
}

export interface SqlPreparedStatement {
  bind(...values: unknown[]): SqlPreparedStatement;
  first<T>(columnName?: string): Promise<T | null>;
  all<T>(): Promise<SqlAllResult<T>>;
  run(): Promise<SqlRunResult>;
}

export interface SqlDatabase {
  prepare(query: string): SqlPreparedStatement;
}

export interface AppBindings {
  DB: SqlDatabase;
  JWT_SECRET: string;
  REFRESH_TOKEN_SECRET: string;
  ADMIN_EMAIL?: string;
  ADMIN_PASSWORD?: string;
  ACCESS_TOKEN_EXPIRES_IN?: string;
  REFRESH_TOKEN_EXPIRES_IN?: string;
  OTP_TOKEN_EXPIRATION_MINUTES?: string;
  R2_ACCESS_KEY_ID?: string;
  R2_SECRET_ACCESS_KEY?: string;
  R2_BUCKET_NAME?: string;
  R2_ACCOUNT_ID?: string;
  R2_PUBLIC_URL?: string;
  R2_UPLOAD_EXPIRES_IN?: string;
  ICON_UPLOAD_BASE_URL?: string;
  ICON_PUBLIC_BASE_URL?: string;
  REDIS_AVAILABLE?: string;
  ALLOWED_ORIGINS?: string;
  WEB_URL?: string;
}

export interface AuthenticatedUser {
  userId: string;
  userName: string;
  email: string;
  roles: string[];
  securityStamp: string;
}

export type AppEnv = {
  Bindings: AppBindings;
  Variables: {
    traceId: string;
    currentUser: AuthenticatedUser;
  };
};
