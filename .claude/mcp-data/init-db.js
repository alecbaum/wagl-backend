const Database = require('better-sqlite3');
const fs = require('fs');
const path = require('path');

console.log('Initializing MCP databases...');

// Initialize agent knowledge database
const knowledgeDb = new Database('./db/agent-knowledge.db');
const initDbSql = fs.readFileSync('./init-db.sql', 'utf8');

// Split SQL statements and execute them
const statements = initDbSql.split(';').filter(stmt => stmt.trim());
statements.forEach(statement => {
    if (statement.trim()) {
        try {
            knowledgeDb.exec(statement);
        } catch (error) {
            console.log(`Skipping statement (may already exist): ${error.message}`);
        }
    }
});

console.log('✓ Agent knowledge database initialized');

// Initialize audit trail database
const auditDb = new Database('./db/audit-trail.db');
const initAuditSql = fs.readFileSync('./init-audit.sql', 'utf8');

const auditStatements = initAuditSql.split(';').filter(stmt => stmt.trim());
auditStatements.forEach(statement => {
    if (statement.trim()) {
        try {
            auditDb.exec(statement);
        } catch (error) {
            console.log(`Skipping statement (may already exist): ${error.message}`);
        }
    }
});

console.log('✓ Audit trail database initialized');

// Insert some test data
const insertDecision = knowledgeDb.prepare(`
    INSERT OR IGNORE INTO decisions (agent_name, decision_type, title, description, rationale)
    VALUES (?, ?, ?, ?, ?)
`);

insertDecision.run('system', 'architecture', 'MCP Memory System Setup', 
    'Implemented local MCP servers for agent memory and coordination',
    'Enables persistent memory across sessions and better agent coordination');

const insertPattern = knowledgeDb.prepare(`
    INSERT OR IGNORE INTO patterns (pattern_name, category, description, discovered_by)
    VALUES (?, ?, ?, ?)
`);

insertPattern.run('MCP Multi-Agent Memory', 'system', 
    'Using MCP servers for agent memory coordination',
    'system');

console.log('✓ Test data inserted');

// Close databases
knowledgeDb.close();
auditDb.close();

console.log('🧠 MCP memory system fully initialized and ready!');