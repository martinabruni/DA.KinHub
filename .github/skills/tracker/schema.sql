PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS id_sequences (
    prefix TEXT PRIMARY KEY,
    next_value INTEGER NOT NULL CHECK (next_value > 0)
);

INSERT OR IGNORE INTO id_sequences (prefix, next_value) VALUES
    ('FEAT', 1),
    ('TASK', 1),
    ('BUG', 1),
    ('CR', 1),
    ('RFC', 1);

CREATE TABLE IF NOT EXISTS features (
    id TEXT PRIMARY KEY,
    slug TEXT NOT NULL UNIQUE,
    title TEXT NOT NULL,
    summary TEXT,
    status TEXT NOT NULL DEFAULT 'backlog' CHECK (status IN ('backlog', 'planned', 'in-progress', 'blocked', 'implemented', 'validated', 'archived')),
    priority TEXT NOT NULL DEFAULT 'medium' CHECK (priority IN ('low', 'medium', 'high', 'critical')),
    source_request TEXT,
    architecture TEXT,
    dependencies TEXT,
    risks TEXT,
    acceptance_criteria TEXT,
    implementation_phases TEXT,
    impacted_files_modules TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS work_items (
    id TEXT PRIMARY KEY,
    feature_id TEXT NOT NULL REFERENCES features (id) ON DELETE CASCADE,
    parent_work_item_id TEXT REFERENCES work_items (id) ON DELETE SET NULL,
    type TEXT NOT NULL CHECK (type IN ('task', 'bug', 'change-request', 'research')),
    title TEXT NOT NULL,
    summary TEXT,
    status TEXT NOT NULL DEFAULT 'backlog' CHECK (status IN ('backlog', 'planned', 'in-progress', 'blocked', 'implemented', 'validated', 'archived')),
    priority TEXT NOT NULL DEFAULT 'medium' CHECK (priority IN ('low', 'medium', 'high', 'critical')),
    source_request TEXT,
    implementation_notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS work_item_links (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_work_item_id TEXT NOT NULL REFERENCES work_items (id) ON DELETE CASCADE,
    target_work_item_id TEXT NOT NULL REFERENCES work_items (id) ON DELETE CASCADE,
    relation_type TEXT NOT NULL CHECK (relation_type IN ('related', 'blocks', 'depends-on', 'duplicates')),
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (source_work_item_id, target_work_item_id, relation_type),
    CHECK (source_work_item_id <> target_work_item_id)
);

CREATE TABLE IF NOT EXISTS work_item_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL CHECK (entity_type IN ('feature', 'work_item', 'work_item_link')),
    entity_id TEXT NOT NULL,
    action TEXT NOT NULL CHECK (action IN ('created', 'updated', 'deleted', 'linked', 'unlinked')),
    snapshot TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_features_status ON features (status);
CREATE INDEX IF NOT EXISTS idx_features_priority ON features (priority);
CREATE INDEX IF NOT EXISTS idx_work_items_feature ON work_items (feature_id);
CREATE INDEX IF NOT EXISTS idx_work_items_parent ON work_items (parent_work_item_id);
CREATE INDEX IF NOT EXISTS idx_work_items_status ON work_items (status);
CREATE INDEX IF NOT EXISTS idx_work_items_type ON work_items (type);
CREATE INDEX IF NOT EXISTS idx_work_item_links_source ON work_item_links (source_work_item_id);
CREATE INDEX IF NOT EXISTS idx_work_item_links_target ON work_item_links (target_work_item_id);
CREATE INDEX IF NOT EXISTS idx_work_item_history_entity ON work_item_history (entity_id, created_at DESC);
