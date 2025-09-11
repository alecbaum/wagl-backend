-- Audit Trail Database Schema
-- Run this against audit-trail.db

CREATE TABLE IF NOT EXISTS agent_actions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    agent_name TEXT NOT NULL,
    action_type TEXT NOT NULL,
    description TEXT,
    input_context TEXT,
    output_summary TEXT,
    duration_ms INTEGER,
    success BOOLEAN DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS agent_interactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_agent TEXT NOT NULL,
    to_agent TEXT NOT NULL,
    interaction_type TEXT NOT NULL,
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_actions_agent ON agent_actions(agent_name);
CREATE INDEX IF NOT EXISTS idx_actions_timestamp ON agent_actions(created_at);
CREATE INDEX IF NOT EXISTS idx_interactions_agents ON agent_interactions(from_agent, to_agent);