import Database from "better-sqlite3";
import type { SqlAllResult, SqlDatabase, SqlPreparedStatement, SqlRunResult } from "../types";

export class FakeD1Database implements SqlDatabase {
  private readonly database: Database.Database;

  constructor() {
    this.database = new Database(":memory:");
    this.database.pragma("foreign_keys = ON");
  }

  prepare(query: string): SqlPreparedStatement {
    return new FakePreparedStatement(this.database, query);
  }
}

class FakePreparedStatement implements SqlPreparedStatement {
  constructor(
    private readonly database: Database.Database,
    private readonly query: string,
    private readonly values: unknown[] = []
  ) {}

  bind(...values: unknown[]) {
    return new FakePreparedStatement(this.database, this.query, values);
  }

  async first<T>(columnName?: string) {
    const row = this.database.prepare(this.query).get(...this.values) as Record<string, T> | undefined;
    if (!row) return null;
    if (columnName) return (row[columnName] ?? null) as T | null;
    return row as T;
  }

  async all<T>(): Promise<SqlAllResult<T>> {
    const results = this.database.prepare(this.query).all(...this.values) as T[];
    return { success: true, results };
  }

  async run(): Promise<SqlRunResult> {
    const result = this.database.prepare(this.query).run(...this.values);
    return {
      success: true,
      meta: {
        changes: result.changes,
        last_row_id: Number(result.lastInsertRowid)
      }
    };
  }
}
