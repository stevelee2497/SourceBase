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
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_time_sheets_user_date_project ON time_sheets(user_id, date, project);
CREATE INDEX IF NOT EXISTS idx_time_sheets_user_date ON time_sheets(user_id, date);
CREATE INDEX IF NOT EXISTS idx_wallets_user_id ON wallets(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_user_id ON transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_wallet_id ON transactions(wallet_id);
CREATE INDEX IF NOT EXISTS idx_transactions_date ON transactions(date);
