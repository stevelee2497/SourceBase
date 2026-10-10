CREATE TABLE wallets (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  name TEXT NOT NULL COLLATE NOCASE,
  initial_balance REAL NOT NULL DEFAULT 0,
  currency TEXT NOT NULL,
  icon TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT
);
CREATE INDEX ix_wallets_user_id ON wallets(user_id);

CREATE TABLE categories (
  id TEXT PRIMARY KEY,
  user_id TEXT,
  name TEXT NOT NULL COLLATE NOCASE,
  type TEXT NOT NULL CHECK (type IN ('Income', 'Expense')),
  icon TEXT,
  is_system INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT
);
CREATE INDEX ix_categories_user_id ON categories(user_id);

CREATE TABLE icons (
  id TEXT PRIMARY KEY,
  user_id TEXT,
  value TEXT NOT NULL,
  name TEXT NOT NULL COLLATE NOCASE,
  grp TEXT NOT NULL CHECK (grp IN ('Wallet', 'Category', 'General')),
  sort_order INTEGER NOT NULL DEFAULT 0,
  is_system INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT
);
CREATE INDEX ix_icons_user_id ON icons(user_id);

CREATE TABLE transactions (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  wallet_id TEXT NOT NULL REFERENCES wallets(id) ON DELETE CASCADE,
  category_id TEXT REFERENCES categories(id),
  amount REAL NOT NULL,
  type TEXT NOT NULL CHECK (type IN ('Income', 'Expense')),
  date TEXT NOT NULL,
  note TEXT,
  is_transfer INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT
);
CREATE INDEX ix_transactions_user_id ON transactions(user_id);
CREATE INDEX ix_transactions_wallet_id ON transactions(wallet_id);
CREATE INDEX ix_transactions_category_id ON transactions(category_id);
CREATE INDEX ix_transactions_date ON transactions(date);

CREATE TABLE transfers (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  from_wallet_id TEXT NOT NULL REFERENCES wallets(id) ON DELETE CASCADE,
  to_wallet_id TEXT NOT NULL REFERENCES wallets(id) ON DELETE CASCADE,
  from_transaction_id TEXT NOT NULL,
  to_transaction_id TEXT NOT NULL,
  amount REAL NOT NULL,
  date TEXT NOT NULL,
  note TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT
);
CREATE INDEX ix_transfers_user_id ON transfers(user_id);
CREATE INDEX ix_transfers_from_wallet_id ON transfers(from_wallet_id);
CREATE INDEX ix_transfers_to_wallet_id ON transfers(to_wallet_id);
