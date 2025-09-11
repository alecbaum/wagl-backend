const Database = require('better-sqlite3');
const fs = require('fs');

console.log('🧪 Testing MCP Memory System...\n');

// Test 1: Knowledge Database
console.log('1. Testing Knowledge Database...');
const knowledgeDb = new Database('./db/agent-knowledge.db');

try {
    const decisions = knowledgeDb.prepare('SELECT * FROM decisions').all();
    console.log(`   ✓ Found ${decisions.length} decisions`);
    
    const patterns = knowledgeDb.prepare('SELECT * FROM patterns').all();
    console.log(`   ✓ Found ${patterns.length} patterns`);
    
    // Test FTS search
    const searchResult = knowledgeDb.prepare(`
        SELECT d.* FROM decisions d 
        JOIN decisions_fts fts ON d.id = fts.rowid 
        WHERE decisions_fts MATCH ?
    `).all('memory');
    console.log(`   ✓ FTS search found ${searchResult.length} results`);
    
} catch (error) {
    console.log(`   ❌ Knowledge DB error: ${error.message}`);
}

knowledgeDb.close();

// Test 2: Audit Database
console.log('\n2. Testing Audit Database...');
const auditDb = new Database('./db/audit-trail.db');

try {
    // Insert test action
    const insertAction = auditDb.prepare(`
        INSERT INTO agent_actions (agent_name, action_type, description, success)
        VALUES (?, ?, ?, ?)
    `);
    
    insertAction.run('test-agent', 'system-test', 'Testing MCP system initialization', true);
    
    const actions = auditDb.prepare('SELECT * FROM agent_actions').all();
    console.log(`   ✓ Found ${actions.length} logged actions`);
    
} catch (error) {
    console.log(`   ❌ Audit DB error: ${error.message}`);
}

auditDb.close();

// Test 3: Filesystem Structure
console.log('\n3. Testing Filesystem Structure...');
const directories = ['artifacts', 'decisions', 'agent-outputs'];

directories.forEach(dir => {
    if (fs.existsSync(dir)) {
        console.log(`   ✓ Directory exists: ${dir}`);
    } else {
        console.log(`   ❌ Missing directory: ${dir}`);
    }
});

// Test 4: MCP Configuration
console.log('\n4. Testing MCP Configuration...');
if (fs.existsSync('../mcp_settings.json')) {
    console.log('   ✓ MCP settings file exists');
    try {
        const config = JSON.parse(fs.readFileSync('../mcp_settings.json', 'utf8'));
        const serverCount = Object.keys(config.mcpServers || {}).length;
        console.log(`   ✓ Found ${serverCount} MCP servers configured`);
    } catch (error) {
        console.log(`   ❌ MCP config error: ${error.message}`);
    }
} else {
    console.log('   ❌ MCP settings file missing');
}

console.log('\n🎉 MCP Memory System Test Complete!\n');

// Show usage examples
console.log('📚 Usage Examples:');
console.log('');
console.log('// Query recent decisions');
console.log('SELECT * FROM decisions ORDER BY created_at DESC LIMIT 5;');
console.log('');
console.log('// Search for patterns by category');
console.log('SELECT * FROM patterns WHERE category = "frontend";');
console.log('');
console.log('// Log agent action');
console.log('INSERT INTO agent_actions (agent_name, action_type, description)');
console.log('VALUES ("frontend-developer", "component_created", "Built login form");');
console.log('');