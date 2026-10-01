import type { AppBindings, SqlDatabase } from "../types";
import { hashPassword } from "./security";

export const roles = {
  admin: "Admin",
  user: "User"
} as const;

const databaseBootstrap = new WeakMap<SqlDatabase, Promise<void>>();

export const baseSchemaStatements = [
  `CREATE TABLE IF NOT EXISTS users (
      id TEXT PRIMARY KEY,
      user_name TEXT NOT NULL COLLATE NOCASE UNIQUE,
      email TEXT NOT NULL COLLATE NOCASE UNIQUE,
      email_confirmed INTEGER NOT NULL DEFAULT 0,
      password_hash TEXT NOT NULL,
      security_stamp TEXT NOT NULL,
      phone_number TEXT NULL,
      first_name TEXT NULL,
      last_name TEXT NULL,
      avatar_url TEXT NULL,
      default_todo_list_id TEXT NULL,
      otp_code TEXT NULL,
      otp_code_expires_on TEXT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS roles (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL COLLATE NOCASE UNIQUE,
      description TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS user_roles (
      user_id TEXT NOT NULL,
      role_id TEXT NOT NULL,
      PRIMARY KEY (user_id, role_id),
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS emails (
      id TEXT PRIMARY KEY,
      "to" TEXT NOT NULL,
      subject TEXT NOT NULL,
      body TEXT NOT NULL,
      sent_on TEXT NOT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS audit_histories (
      id TEXT PRIMARY KEY,
      author TEXT NULL,
      action TEXT NOT NULL,
      action_on TEXT NOT NULL,
      entity_type TEXT NOT NULL,
      entity_id TEXT NOT NULL,
      current TEXT NULL,
      original TEXT NULL,
      changes TEXT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS todo_lists (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      user_id TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS todo_items (
      id TEXT PRIMARY KEY,
      title TEXT NOT NULL,
      date TEXT NOT NULL,
      status TEXT NOT NULL,
      user_id TEXT NOT NULL,
      todo_list_id TEXT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (todo_list_id) REFERENCES todo_lists(id)
  )`,
  `CREATE TABLE IF NOT EXISTS time_sheets (
      id TEXT PRIMARY KEY,
      date TEXT NOT NULL,
      project TEXT NOT NULL,
      hours REAL NOT NULL,
      user_id TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS notifications (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      event TEXT NOT NULL,
      title TEXT NOT NULL,
      message TEXT NOT NULL,
      data TEXT NOT NULL,
      is_read INTEGER NOT NULL DEFAULT 0,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS gold_prices (
      id TEXT PRIMARY KEY,
      source TEXT NOT NULL,
      buy_price REAL NOT NULL,
      sell_price REAL NOT NULL,
      recorded_at TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS machines (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL COLLATE NOCASE,
      alias TEXT NULL COLLATE NOCASE,
      status TEXT NOT NULL,
      last_reported_on TEXT NULL,
      user_id TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS machine_commands (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      machine_id TEXT NOT NULL,
      type TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (machine_id) REFERENCES machines(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS habits (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL COLLATE NOCASE,
      icon TEXT NULL,
      user_id TEXT NULL,
      is_system INTEGER NOT NULL DEFAULT 0,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS habit_logs (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      habit_id TEXT NULL,
      habit_name TEXT NULL COLLATE NOCASE,
      action TEXT NOT NULL,
      occurred_at TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS icons (
      id TEXT PRIMARY KEY,
      value TEXT NOT NULL,
      name TEXT NOT NULL,
      group_name TEXT NOT NULL,
      sort_order INTEGER NOT NULL,
      is_system INTEGER NOT NULL DEFAULT 0,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL
  )`,
  `CREATE TABLE IF NOT EXISTS categories (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      type TEXT NOT NULL,
      icon TEXT NULL,
      user_id TEXT NULL,
      is_system INTEGER NOT NULL DEFAULT 0,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      CHECK (type IN ('Income', 'Expense'))
  )`,
  `CREATE TABLE IF NOT EXISTS wallets (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      initial_balance REAL NOT NULL DEFAULT 0,
      currency TEXT NOT NULL,
      icon TEXT NULL,
      user_id TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  `CREATE TABLE IF NOT EXISTS transactions (
      id TEXT PRIMARY KEY,
      amount REAL NOT NULL,
      type TEXT NOT NULL,
      date TEXT NOT NULL,
      note TEXT NULL,
      wallet_id TEXT NOT NULL,
      category_id TEXT NULL,
      user_id TEXT NOT NULL,
      is_transfer INTEGER NOT NULL DEFAULT 0,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (wallet_id) REFERENCES wallets(id) ON DELETE CASCADE,
      FOREIGN KEY (category_id) REFERENCES categories(id),
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      CHECK (type IN ('Income', 'Expense'))
  )`,
  `CREATE TABLE IF NOT EXISTS transfers (
      id TEXT PRIMARY KEY,
      from_wallet_id TEXT NOT NULL,
      to_wallet_id TEXT NOT NULL,
      amount REAL NOT NULL,
      date TEXT NOT NULL,
      note TEXT NULL,
      from_transaction_id TEXT NOT NULL,
      to_transaction_id TEXT NOT NULL,
      user_id TEXT NOT NULL,
      created_on TEXT NOT NULL,
      created_by TEXT NULL,
      updated_on TEXT NULL,
      updated_by TEXT NULL,
      FOREIGN KEY (from_wallet_id) REFERENCES wallets(id) ON DELETE CASCADE,
      FOREIGN KEY (to_wallet_id) REFERENCES wallets(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
  )`,
  "CREATE INDEX IF NOT EXISTS idx_users_email ON users(email)",
  "CREATE INDEX IF NOT EXISTS idx_users_user_name ON users(user_name)",
  "CREATE INDEX IF NOT EXISTS idx_audit_histories_action_on ON audit_histories(action_on)",
  "CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_todo_lists_user_id ON todo_lists(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_todo_items_user_id ON todo_items(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_todo_items_todo_list_id ON todo_items(todo_list_id)",
  "CREATE INDEX IF NOT EXISTS idx_todo_items_user_date ON todo_items(user_id, date)",
  "CREATE UNIQUE INDEX IF NOT EXISTS idx_time_sheets_user_date_project ON time_sheets(user_id, date, project)",
  "CREATE INDEX IF NOT EXISTS idx_time_sheets_user_date ON time_sheets(user_id, date)",
  "CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_notifications_user_id_is_read ON notifications(user_id, is_read)",
  "CREATE UNIQUE INDEX IF NOT EXISTS idx_gold_prices_source_recorded_at ON gold_prices(source, recorded_at)",
  "CREATE UNIQUE INDEX IF NOT EXISTS idx_machines_user_id_name ON machines(user_id, name)",
  "CREATE INDEX IF NOT EXISTS idx_machine_commands_user_id ON machine_commands(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_machine_commands_machine_id ON machine_commands(machine_id)",
  "CREATE INDEX IF NOT EXISTS idx_habits_user_id ON habits(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_habit_logs_user_id ON habit_logs(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_habit_logs_habit_id ON habit_logs(habit_id)",
  "CREATE INDEX IF NOT EXISTS idx_habit_logs_occurred_at ON habit_logs(occurred_at)",
  "CREATE INDEX IF NOT EXISTS idx_icons_group_sort_order ON icons(group_name, sort_order)",
  "CREATE INDEX IF NOT EXISTS idx_categories_user_id ON categories(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_categories_type ON categories(type)",
  "CREATE INDEX IF NOT EXISTS idx_wallets_user_id ON wallets(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_transactions_user_id ON transactions(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_transactions_wallet_id ON transactions(wallet_id)",
  "CREATE INDEX IF NOT EXISTS idx_transactions_category_id ON transactions(category_id)",
  "CREATE INDEX IF NOT EXISTS idx_transactions_date ON transactions(date)",
  "CREATE INDEX IF NOT EXISTS idx_transactions_is_transfer ON transactions(is_transfer)",
  "CREATE INDEX IF NOT EXISTS idx_transfers_user_id ON transfers(user_id)",
  "CREATE INDEX IF NOT EXISTS idx_transfers_from_wallet_id ON transfers(from_wallet_id)",
  "CREATE INDEX IF NOT EXISTS idx_transfers_to_wallet_id ON transfers(to_wallet_id)",
  "CREATE INDEX IF NOT EXISTS idx_transfers_date ON transfers(date)"
];

export async function bootstrapBaseSchema(db: SqlDatabase) {
  for (const statement of baseSchemaStatements) await db.prepare(statement).run();
  await ensureSystemHabits(db);
  await ensureSystemIcons(db);
}

export function ensureDatabaseReady(bindings: AppBindings) {
  const existing = databaseBootstrap.get(bindings.DB);
  if (existing) return existing;

  const setup = (async () => {
    await bootstrapBaseSchema(bindings.DB);
    await seedBaseData(bindings.DB, bindings);
  })();

  databaseBootstrap.set(bindings.DB, setup);
  return setup;
}

export async function seedBaseData(db: SqlDatabase, bindings: AppBindings) {
  const now = nowIso();
  await ensureRole(db, roles.admin, "Admin role", now);
  await ensureRole(db, roles.user, "User role", now);
  await ensureSystemHabits(db);
  await ensureSystemIcons(db);
  await ensureSystemCategories(db, now);

  const adminEmail = (bindings.ADMIN_EMAIL ?? "admin@yopmail.com").trim();
  const adminPassword = (bindings.ADMIN_PASSWORD ?? "ChangeMe123!").trim();
  const adminExists = await db.prepare(`SELECT id FROM users WHERE lower(email) = lower(?)`).bind(adminEmail).first<{ id: string }>();

  if (adminExists) return;

  const adminId = crypto.randomUUID();
  const adminRole = await db.prepare(`SELECT id FROM roles WHERE name = ?`).bind(roles.admin).first<{ id: string }>();
  const passwordHash = await hashPassword(adminPassword);

  await db.prepare(
    `INSERT INTO users (
        id, user_name, email, email_confirmed, password_hash, security_stamp,
        created_on, created_by
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    ON CONFLICT(email) DO NOTHING`
  )
    .bind(adminId, adminEmail, adminEmail, 1, passwordHash, crypto.randomUUID(), now, adminEmail)
    .run();

  await db.prepare(`INSERT OR IGNORE INTO user_roles (user_id, role_id) VALUES (?, ?)`)
    .bind(adminId, adminRole?.id ?? crypto.randomUUID())
    .run();
}

export async function getUserRoles(db: SqlDatabase, userId: string) {
  const result = await db.prepare(
    `SELECT roles.name
     FROM roles
     INNER JOIN user_roles ON user_roles.role_id = roles.id
     WHERE user_roles.user_id = ?
     ORDER BY roles.name`
  )
    .bind(userId)
    .all<{ name: string }>();

  return result.results.map(row => row.name);
}

export async function insertEmail(db: SqlDatabase, to: string, subject: string, body: string) {
  await db.prepare(`INSERT INTO emails (id, "to", subject, body, sent_on) VALUES (?, ?, ?, ?, ?)`)
    .bind(crypto.randomUUID(), to, subject, body, nowIso())
    .run();
}

export function nowIso() {
  return new Date().toISOString();
}

async function ensureRole(db: SqlDatabase, name: string, description: string, createdOn: string) {
  const existing = await db.prepare(`SELECT id FROM roles WHERE name = ?`).bind(name).first<{ id: string }>();
  if (existing) return existing.id;

  const id = crypto.randomUUID();
  await db.prepare(`INSERT INTO roles (id, name, description, created_on, created_by) VALUES (?, ?, ?, ?, ?)`)
    .bind(id, name, description, createdOn, "system")
    .run();
  return id;
}

const systemHabits = [
  ["00000000-0000-0000-0000-000000000101", "Walk", "🚶"],
  ["00000000-0000-0000-0000-000000000102", "Drink Water", "💧"],
  ["00000000-0000-0000-0000-000000000103", "Read", "📖"],
  ["00000000-0000-0000-0000-000000000104", "Exercise", "🏋️"],
  ["00000000-0000-0000-0000-000000000105", "Meditate", "🧘"]
] as const;

const systemIcons = [
  ["00000000-0000-0000-0000-000000000201", "💳", "Credit Card", "Wallet", 1],
  ["00000000-0000-0000-0000-000000000202", "🏦", "Bank", "Wallet", 2],
  ["00000000-0000-0000-0000-000000000203", "💵", "Cash", "Wallet", 3],
  ["00000000-0000-0000-0000-000000000204", "👛", "Wallet", "Wallet", 4],
  ["00000000-0000-0000-0000-000000000205", "💰", "Savings", "Wallet", 5],
  ["00000000-0000-0000-0000-000000000206", "📈", "Investment", "Wallet", 6],
  ["00000000-0000-0000-0000-000000000207", "🏠", "Property", "Wallet", 7],
  ["00000000-0000-0000-0000-000000000208", "🚀", "Goals", "Wallet", 8],
  ["00000000-0000-0000-0000-000000000209", "💎", "Premium", "Wallet", 9],
  ["00000000-0000-0000-0000-000000000210", "🏧", "ATM", "Wallet", 10],
  ["00000000-0000-0000-0000-000000000211", "💹", "Trading", "Wallet", 11],
  ["00000000-0000-0000-0000-000000000212", "🌏", "Foreign", "Wallet", 12],
  ["00000000-0000-0000-0000-000000000301", "🍔", "Food", "Category", 1],
  ["00000000-0000-0000-0000-000000000302", "☕", "Cafe", "Category", 2],
  ["00000000-0000-0000-0000-000000000303", "🚗", "Transport", "Category", 3],
  ["00000000-0000-0000-0000-000000000304", "✈️", "Travel", "Category", 4],
  ["00000000-0000-0000-0000-000000000305", "🛍️", "Shopping", "Category", 5],
  ["00000000-0000-0000-0000-000000000306", "💡", "Utilities", "Category", 6],
  ["00000000-0000-0000-0000-000000000307", "❤️", "Health", "Category", 7],
  ["00000000-0000-0000-0000-000000000308", "🎬", "Entertainment", "Category", 8],
  ["00000000-0000-0000-0000-000000000309", "📚", "Education", "Category", 9],
  ["00000000-0000-0000-0000-000000000310", "🎁", "Gift", "Category", 10],
  ["00000000-0000-0000-0000-000000000311", "💻", "Freelance", "Category", 11],
  ["00000000-0000-0000-0000-000000000312", "💼", "Salary", "Category", 12],
  ["00000000-0000-0000-0000-000000000313", "📦", "Other", "Category", 13],
  ["00000000-0000-0000-0000-000000000314", "🛒", "Groceries", "Category", 14],
  ["00000000-0000-0000-0000-000000000315", "🏥", "Medical", "Category", 15],
  ["00000000-0000-0000-0000-000000000316", "🎓", "Tuition", "Category", 16],
  ["00000000-0000-0000-0000-000000000317", "⚽", "Sports", "Category", 17],
  ["00000000-0000-0000-0000-000000000318", "🐾", "Pets", "Category", 18],
  ["00000000-0000-0000-0000-000000000319", "🔧", "Maintenance", "Category", 19],
  ["00000000-0000-0000-0000-000000000320", "🎮", "Gaming", "Category", 20],
  ["00000000-0000-0000-0000-000000000401", "⭐", "Favourite", "General", 1],
  ["00000000-0000-0000-0000-000000000402", "📌", "Pinned", "General", 2],
  ["00000000-0000-0000-0000-000000000403", "🔔", "Alert", "General", 3],
  ["00000000-0000-0000-0000-000000000404", "🌟", "Special", "General", 4],
  ["00000000-0000-0000-0000-000000000405", "🏷️", "Tag", "General", 5],
  ["00000000-0000-0000-0000-000000000406", "💬", "Note", "General", 6],
  ["00000000-0000-0000-0000-000000000407", "🔑", "Key", "General", 7],
  ["00000000-0000-0000-0000-000000000408", "📊", "Stats", "General", 8]
] as const;

const defaultSystemCategories = [
  ["Salary", "Income", "💼"],
  ["Freelance", "Income", "💻"],
  ["Investment", "Income", "📈"],
  ["Gift", "Income", "🎁"],
  ["Other Income", "Income", "💰"],
  ["Food & Drink", "Expense", "🍔"],
  ["Transport", "Expense", "🚗"],
  ["Shopping", "Expense", "🛍️"],
  ["Bills & Utilities", "Expense", "💡"],
  ["Health", "Expense", "❤️"],
  ["Entertainment", "Expense", "🎬"],
  ["Education", "Expense", "📚"],
  ["Travel", "Expense", "✈️"],
  ["Other Expense", "Expense", "📦"]
] as const;

async function ensureSystemHabits(db: SqlDatabase) {
  for (const [id, name, icon] of systemHabits) {
    await db.prepare(
      `INSERT OR IGNORE INTO habits (id, name, icon, user_id, is_system, created_on, created_by)
       VALUES (?, ?, ?, NULL, 1, ?, ?)`
    )
      .bind(id, name, icon, "2026-01-01T00:00:00.000Z", "system")
      .run();
  }
}

async function ensureSystemIcons(db: SqlDatabase) {
  for (const [id, value, name, groupName, sortOrder] of systemIcons) {
    await db.prepare(
      `INSERT OR IGNORE INTO icons (id, value, name, group_name, sort_order, is_system, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, 1, ?, ?)`
    )
      .bind(id, value, name, groupName, sortOrder, "2026-01-01T00:00:00.000Z", "system")
      .run();
  }
}

async function ensureSystemCategories(db: SqlDatabase, createdOn: string) {
  for (const [name, type, icon] of defaultSystemCategories) {
    const existing = await db.prepare(
      `SELECT id
       FROM categories
       WHERE is_system = 1 AND name = ? AND type = ?`
    )
      .bind(name, type)
      .first<{ id: string }>();

    if (existing) continue;

    await db.prepare(
      `INSERT INTO categories (id, name, type, icon, user_id, is_system, created_on, created_by)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    )
      .bind(crypto.randomUUID(), name, type, icon, null, 1, createdOn, "system")
      .run();
  }
}
