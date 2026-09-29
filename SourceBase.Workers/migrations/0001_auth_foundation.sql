CREATE TABLE IF NOT EXISTS users (
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
);

CREATE TABLE IF NOT EXISTS roles (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    description TEXT NOT NULL,
    created_on TEXT NOT NULL,
    created_by TEXT NULL,
    updated_on TEXT NULL,
    updated_by TEXT NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id TEXT NOT NULL,
    role_id TEXT NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS emails (
    id TEXT PRIMARY KEY,
    "to" TEXT NOT NULL,
    subject TEXT NOT NULL,
    body TEXT NOT NULL,
    sent_on TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS todo_lists (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    user_id TEXT NOT NULL,
    created_on TEXT NOT NULL,
    created_by TEXT NULL,
    updated_on TEXT NULL,
    updated_by TEXT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS todo_items (
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
);

CREATE TABLE IF NOT EXISTS time_sheets (
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
);

CREATE TABLE IF NOT EXISTS notifications (
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
);

CREATE TABLE IF NOT EXISTS habits (
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
);

CREATE TABLE IF NOT EXISTS habit_logs (
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
);

CREATE TABLE IF NOT EXISTS icons (
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
);

CREATE TABLE IF NOT EXISTS categories (
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
);

CREATE TABLE IF NOT EXISTS wallets (
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
);

CREATE TABLE IF NOT EXISTS transactions (
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
);

CREATE TABLE IF NOT EXISTS transfers (
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
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_user_name ON users(user_name);
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_todo_lists_user_id ON todo_lists(user_id);
CREATE INDEX IF NOT EXISTS idx_todo_items_user_id ON todo_items(user_id);
CREATE INDEX IF NOT EXISTS idx_todo_items_todo_list_id ON todo_items(todo_list_id);
CREATE INDEX IF NOT EXISTS idx_todo_items_user_date ON todo_items(user_id, date);
CREATE UNIQUE INDEX IF NOT EXISTS idx_time_sheets_user_date_project ON time_sheets(user_id, date, project);
CREATE INDEX IF NOT EXISTS idx_time_sheets_user_date ON time_sheets(user_id, date);
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_habits_user_id ON habits(user_id);
CREATE INDEX IF NOT EXISTS idx_habit_logs_user_id ON habit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_habit_logs_habit_id ON habit_logs(habit_id);
CREATE INDEX IF NOT EXISTS idx_habit_logs_occurred_at ON habit_logs(occurred_at);
CREATE INDEX IF NOT EXISTS idx_icons_group_sort_order ON icons(group_name, sort_order);
CREATE INDEX IF NOT EXISTS idx_categories_user_id ON categories(user_id);
CREATE INDEX IF NOT EXISTS idx_categories_type ON categories(type);
CREATE INDEX IF NOT EXISTS idx_wallets_user_id ON wallets(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_user_id ON transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_wallet_id ON transactions(wallet_id);
CREATE INDEX IF NOT EXISTS idx_transactions_category_id ON transactions(category_id);
CREATE INDEX IF NOT EXISTS idx_transactions_date ON transactions(date);
CREATE INDEX IF NOT EXISTS idx_transactions_is_transfer ON transactions(is_transfer);
CREATE INDEX IF NOT EXISTS idx_transfers_user_id ON transfers(user_id);
CREATE INDEX IF NOT EXISTS idx_transfers_from_wallet_id ON transfers(from_wallet_id);
CREATE INDEX IF NOT EXISTS idx_transfers_to_wallet_id ON transfers(to_wallet_id);
CREATE INDEX IF NOT EXISTS idx_transfers_date ON transfers(date);

INSERT OR IGNORE INTO habits (id, name, icon, user_id, is_system, created_on, created_by) VALUES
    ('00000000-0000-0000-0000-000000000101', 'Walk', '🚶', NULL, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000102', 'Drink Water', '💧', NULL, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000103', 'Read', '📖', NULL, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000104', 'Exercise', '🏋️', NULL, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000105', 'Meditate', '🧘', NULL, 1, '2026-01-01T00:00:00.000Z', 'system');

INSERT OR IGNORE INTO icons (id, value, name, group_name, sort_order, is_system, created_on, created_by) VALUES
    ('00000000-0000-0000-0000-000000000201', '💳', 'Credit Card', 'Wallet', 1, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000202', '🏦', 'Bank', 'Wallet', 2, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000203', '💵', 'Cash', 'Wallet', 3, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000204', '👛', 'Wallet', 'Wallet', 4, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000205', '💰', 'Savings', 'Wallet', 5, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000206', '📈', 'Investment', 'Wallet', 6, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000207', '🏠', 'Property', 'Wallet', 7, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000208', '🚀', 'Goals', 'Wallet', 8, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000209', '💎', 'Premium', 'Wallet', 9, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000210', '🏧', 'ATM', 'Wallet', 10, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000211', '💹', 'Trading', 'Wallet', 11, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000212', '🌏', 'Foreign', 'Wallet', 12, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000301', '🍔', 'Food', 'Category', 1, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000302', '☕', 'Cafe', 'Category', 2, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000303', '🚗', 'Transport', 'Category', 3, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000304', '✈️', 'Travel', 'Category', 4, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000305', '🛍️', 'Shopping', 'Category', 5, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000306', '💡', 'Utilities', 'Category', 6, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000307', '❤️', 'Health', 'Category', 7, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000308', '🎬', 'Entertainment', 'Category', 8, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000309', '📚', 'Education', 'Category', 9, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000310', '🎁', 'Gift', 'Category', 10, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000311', '💻', 'Freelance', 'Category', 11, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000312', '💼', 'Salary', 'Category', 12, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000313', '📦', 'Other', 'Category', 13, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000314', '🛒', 'Groceries', 'Category', 14, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000315', '🏥', 'Medical', 'Category', 15, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000316', '🎓', 'Tuition', 'Category', 16, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000317', '⚽', 'Sports', 'Category', 17, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000318', '🐾', 'Pets', 'Category', 18, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000319', '🔧', 'Maintenance', 'Category', 19, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000320', '🎮', 'Gaming', 'Category', 20, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000401', '⭐', 'Favourite', 'General', 1, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000402', '📌', 'Pinned', 'General', 2, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000403', '🔔', 'Alert', 'General', 3, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000404', '🌟', 'Special', 'General', 4, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000405', '🏷️', 'Tag', 'General', 5, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000406', '💬', 'Note', 'General', 6, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000407', '🔑', 'Key', 'General', 7, 1, '2026-01-01T00:00:00.000Z', 'system'),
    ('00000000-0000-0000-0000-000000000408', '📊', 'Stats', 'General', 8, 1, '2026-01-01T00:00:00.000Z', 'system');
