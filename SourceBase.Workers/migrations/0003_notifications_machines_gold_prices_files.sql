CREATE TABLE IF NOT EXISTS gold_prices (
    id TEXT PRIMARY KEY,
    source TEXT NOT NULL,
    buy_price REAL NOT NULL,
    sell_price REAL NOT NULL,
    recorded_at TEXT NOT NULL,
    created_on TEXT NOT NULL,
    created_by TEXT NULL,
    updated_on TEXT NULL,
    updated_by TEXT NULL
);

CREATE TABLE IF NOT EXISTS machines (
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
);

CREATE TABLE IF NOT EXISTS machine_commands (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    machine_id TEXT NOT NULL,
    type TEXT NOT NULL,
    created_on TEXT NOT NULL,
    created_by TEXT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (machine_id) REFERENCES machines(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_notifications_user_id_is_read ON notifications(user_id, is_read);
CREATE UNIQUE INDEX IF NOT EXISTS idx_gold_prices_source_recorded_at ON gold_prices(source, recorded_at);
CREATE UNIQUE INDEX IF NOT EXISTS idx_machines_user_id_name ON machines(user_id, name);
CREATE INDEX IF NOT EXISTS idx_machine_commands_user_id ON machine_commands(user_id);
CREATE INDEX IF NOT EXISTS idx_machine_commands_machine_id ON machine_commands(machine_id);
