CREATE TABLE IF NOT EXISTS audit_histories (
    id TEXT PRIMARY KEY,
    author TEXT NULL,
    action TEXT NOT NULL,
    action_on TEXT NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    current TEXT NULL,
    original TEXT NULL,
    changes TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_histories_action_on ON audit_histories(action_on);
