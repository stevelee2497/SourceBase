export const newId = () => crypto.randomUUID();
export const now = () => new Date().toISOString();

export const stmt = (db: D1Database, sql: string, ...params: unknown[]) => db.prepare(sql).bind(...params);
export const first = <T>(db: D1Database, sql: string, ...params: unknown[]) => stmt(db, sql, ...params).first<T>();
export const all = async <T>(db: D1Database, sql: string, ...params: unknown[]) => (await stmt(db, sql, ...params).all<T>()).results;
export const run = (db: D1Database, sql: string, ...params: unknown[]) => stmt(db, sql, ...params).run();

export const placeholders = (items: unknown[]) => items.map(() => "?").join(",");
