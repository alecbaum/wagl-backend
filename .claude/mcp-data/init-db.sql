-- Agent Knowledge Database Schema
-- Run this against agent-knowledge.db

-- Architectural decisions table
CREATE TABLE IF NOT EXISTS decisions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    agent_name TEXT NOT NULL,
    decision_type TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    rationale TEXT,
    impact_areas TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    supersedes_id INTEGER,
    FOREIGN KEY (supersedes_id) REFERENCES decisions(id)
);

-- Design patterns table
CREATE TABLE IF NOT EXISTS patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pattern_name TEXT NOT NULL UNIQUE,
    category TEXT NOT NULL,
    description TEXT,
    example_code TEXT,
    use_cases TEXT,
    discovered_by TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Component relationships table
CREATE TABLE IF NOT EXISTS components (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    component_name TEXT NOT NULL UNIQUE,
    component_type TEXT NOT NULL,
    description TEXT,
    dependencies TEXT,
    created_by TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- API specifications table
CREATE TABLE IF NOT EXISTS api_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    endpoint TEXT NOT NULL,
    method TEXT NOT NULL,
    description TEXT,
    request_schema TEXT,
    response_schema TEXT,
    created_by TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(endpoint, method)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_decisions_agent ON decisions(agent_name);
CREATE INDEX IF NOT EXISTS idx_decisions_type ON decisions(decision_type);
CREATE INDEX IF NOT EXISTS idx_patterns_category ON patterns(category);

-- Enable FTS5 for full-text search
CREATE VIRTUAL TABLE IF NOT EXISTS decisions_fts USING fts5(
    title, description, rationale, content='decisions', content_rowid='id'
);

-- Trigger to keep FTS table in sync
CREATE TRIGGER IF NOT EXISTS decisions_fts_insert AFTER INSERT ON decisions BEGIN
    INSERT INTO decisions_fts(rowid, title, description, rationale) 
    VALUES (new.id, new.title, new.description, new.rationale);
END;

CREATE TRIGGER IF NOT EXISTS decisions_fts_delete AFTER DELETE ON decisions BEGIN
    DELETE FROM decisions_fts WHERE rowid = old.id;
END;

CREATE TRIGGER IF NOT EXISTS decisions_fts_update AFTER UPDATE ON decisions BEGIN
    DELETE FROM decisions_fts WHERE rowid = old.id;
    INSERT INTO decisions_fts(rowid, title, description, rationale) 
    VALUES (new.id, new.title, new.description, new.rationale);
END;