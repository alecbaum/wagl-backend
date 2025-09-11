#!/bin/bash
# Memory management utilities for MCP system

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Database paths (from project root)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
KNOWLEDGE_DB="$PROJECT_ROOT/.claude/mcp-data/db/agent-knowledge.db"
AUDIT_DB="$PROJECT_ROOT/.claude/mcp-data/db/audit-trail.db"
MCP_DATA_DIR="$PROJECT_ROOT/.claude/mcp-data"

# Check if databases exist
check_databases() {
    if [ ! -f "$KNOWLEDGE_DB" ]; then
        echo -e "${RED}❌ Knowledge database not found at $KNOWLEDGE_DB${NC}"
        return 1
    fi
    if [ ! -f "$AUDIT_DB" ]; then
        echo -e "${RED}❌ Audit database not found at $AUDIT_DB${NC}"
        return 1
    fi
    return 0
}

# Function to backup databases
backup_memory() {
    echo -e "${BLUE}🔄 Backing up MCP databases...${NC}"
    
    if ! check_databases; then
        return 1
    fi
    
    timestamp=$(date +%Y%m%d_%H%M%S)
    mkdir -p $MCP_DATA_DIR/backups
    
    cp "$KNOWLEDGE_DB" "$MCP_DATA_DIR/backups/agent-knowledge_$timestamp.db"
    cp "$AUDIT_DB" "$MCP_DATA_DIR/backups/audit-trail_$timestamp.db"
    
    echo -e "${GREEN}✓ Backup completed: $timestamp${NC}"
    echo -e "${CYAN}📁 Backup location: $MCP_DATA_DIR/backups/${NC}"
}

# Function to query recent decisions
recent_decisions() {
    echo -e "${PURPLE}🧠 Recent Architectural Decisions:${NC}"
    echo "============================================="
    
    if ! check_databases; then
        return 1
    fi
    
    node -e "
        const Database = require('better-sqlite3');
        const db = new Database('$KNOWLEDGE_DB');
        const decisions = db.prepare(\`
            SELECT datetime(created_at, 'localtime') as time, 
                   agent_name, 
                   decision_type,
                   title,
                   description
            FROM decisions 
            ORDER BY created_at DESC 
            LIMIT 10
        \`).all();
        
        decisions.forEach(d => {
            console.log(\`\${d.time} | \${d.agent_name} | \${d.decision_type}\`);
            console.log(\`  📋 \${d.title}\`);
            if (d.description) console.log(\`  📝 \${d.description.substring(0, 80)}...\`);
            console.log('');
        });
        
        db.close();
    "
}

# Function to show agent activity
agent_activity() {
    echo -e "${YELLOW}👥 Agent Activity (Last 7 Days):${NC}"
    echo "=================================="
    
    if ! check_databases; then
        return 1
    fi
    
    node -e "
        const Database = require('better-sqlite3');
        const db = new Database('$AUDIT_DB');
        const activity = db.prepare(\`
            SELECT agent_name, 
                   COUNT(*) as actions,
                   AVG(CASE WHEN success THEN 1.0 ELSE 0.0 END) as success_rate
            FROM agent_actions 
            WHERE created_at > datetime('now', '-7 days') 
            GROUP BY agent_name 
            ORDER BY actions DESC
        \`).all();
        
        activity.forEach(a => {
            const successPercent = (a.success_rate * 100).toFixed(1);
            console.log(\`\${a.agent_name.padEnd(20)} | \${a.actions.toString().padStart(3)} actions | \${successPercent}% success\`);
        });
        
        db.close();
    "
}

# Function to show patterns
show_patterns() {
    echo -e "${CYAN}🎨 Design Patterns Library:${NC}"
    echo "=========================="
    
    if ! check_databases; then
        return 1
    fi
    
    node -e "
        const Database = require('better-sqlite3');
        const db = new Database('$KNOWLEDGE_DB');
        const patterns = db.prepare(\`
            SELECT pattern_name, 
                   category, 
                   description,
                   discovered_by,
                   datetime(created_at, 'localtime') as created
            FROM patterns 
            ORDER BY category, created_at DESC
        \`).all();
        
        let currentCategory = '';
        patterns.forEach(p => {
            if (p.category !== currentCategory) {
                console.log(\`\\n📂 \${p.category.toUpperCase()}\`);
                console.log('─'.repeat(40));
                currentCategory = p.category;
            }
            console.log(\`  🔧 \${p.pattern_name}\`);
            if (p.description) console.log(\`     \${p.description}\`);
            console.log(\`     By: \${p.discovered_by} | \${p.created}\`);
            console.log('');
        });
        
        db.close();
    "
}

# Function to show registered components
show_components() {
    echo -e "${GREEN}🧩 Registered Components:${NC}"
    echo "========================"
    
    if ! check_databases; then
        return 1
    fi
    
    node -e "
        const Database = require('better-sqlite3');
        const db = new Database('$KNOWLEDGE_DB');
        const components = db.prepare(\`
            SELECT component_name, 
                   component_type, 
                   description,
                   dependencies,
                   created_by,
                   datetime(created_at, 'localtime') as created
            FROM components 
            ORDER BY component_type, created_at DESC
        \`).all();
        
        let currentType = '';
        components.forEach(c => {
            if (c.component_type !== currentType) {
                console.log(\`\\n📦 \${c.component_type.toUpperCase()}\`);
                console.log('─'.repeat(40));
                currentType = c.component_type;
            }
            console.log(\`  🔧 \${c.component_name}\`);
            if (c.description) console.log(\`     \${c.description}\`);
            if (c.dependencies) console.log(\`     Dependencies: \${c.dependencies}\`);
            console.log(\`     By: \${c.created_by} | \${c.created}\`);
            console.log('');
        });
        
        db.close();
    "
}

# Function to show system status
show_status() {
    echo -e "${BLUE}🔍 MCP Memory System Status:${NC}"
    echo "============================"
    
    # Check databases
    if check_databases; then
        echo -e "${GREEN}✓ Databases: Available${NC}"
    else
        echo -e "${RED}❌ Databases: Missing${NC}"
        return 1
    fi
    
    # Check MCP configuration
    if [ -f "$PROJECT_ROOT/.claude/mcp_settings.json" ]; then
        echo -e "${GREEN}✓ MCP Configuration: Found${NC}"
        
        # Count configured servers
        server_count=$(node -e "
            const config = require('$PROJECT_ROOT/.claude/mcp_settings.json');
            console.log(Object.keys(config.mcpServers || {}).length);
        " 2>/dev/null || echo "0")
        
        echo -e "${CYAN}  📡 Configured servers: $server_count${NC}"
    else
        echo -e "${RED}❌ MCP Configuration: Missing${NC}"
    fi
    
    # Check directories
    directories=("artifacts" "decisions" "agent-outputs")
    for dir in "${directories[@]}"; do
        if [ -d "$MCP_DATA_DIR/$dir" ]; then
            echo -e "${GREEN}✓ Directory: $dir${NC}"
        else
            echo -e "${RED}❌ Directory: $dir${NC}"
        fi
    done
    
    # Show statistics
    echo ""
    echo -e "${PURPLE}📊 Statistics:${NC}"
    
    node -e "
        const Database = require('better-sqlite3');
        
        try {
            const knowledgeDb = new Database('$KNOWLEDGE_DB');
            const auditDb = new Database('$AUDIT_DB');
            
            const decisionCount = knowledgeDb.prepare('SELECT COUNT(*) as count FROM decisions').get().count;
            const patternCount = knowledgeDb.prepare('SELECT COUNT(*) as count FROM patterns').get().count;
            const componentCount = knowledgeDb.prepare('SELECT COUNT(*) as count FROM components').get().count;
            const actionCount = auditDb.prepare('SELECT COUNT(*) as count FROM agent_actions').get().count;
            
            console.log(\`  🎯 Decisions: \${decisionCount}\`);
            console.log(\`  🎨 Patterns: \${patternCount}\`);
            console.log(\`  🧩 Components: \${componentCount}\`);
            console.log(\`  📝 Actions logged: \${actionCount}\`);
            
            knowledgeDb.close();
            auditDb.close();
        } catch (error) {
            console.log('  ❌ Could not retrieve statistics');
        }
    "
}

# Function to initialize test data
init_test_data() {
    echo -e "${YELLOW}🧪 Initializing test data...${NC}"
    
    if ! check_databases; then
        return 1
    fi
    
    node -e "
        const Database = require('better-sqlite3');
        const knowledgeDb = new Database('$KNOWLEDGE_DB');
        const auditDb = new Database('$AUDIT_DB');
        
        // Add test decisions
        const insertDecision = knowledgeDb.prepare(\`
            INSERT OR IGNORE INTO decisions (agent_name, decision_type, title, description, rationale)
            VALUES (?, ?, ?, ?, ?)
        \`);
        
        insertDecision.run('backend-architect', 'database', 'Use PostgreSQL with pgvector', 
            'Selected PostgreSQL for Bible app with vector search capabilities',
            'Supports ACID transactions, JSON storage, and semantic search');
            
        insertDecision.run('frontend-developer', 'framework', 'React with TypeScript',
            'Chose React with TypeScript for frontend development',
            'Type safety and component reusability');
        
        // Add test patterns
        const insertPattern = knowledgeDb.prepare(\`
            INSERT OR IGNORE INTO patterns (pattern_name, category, description, discovered_by)
            VALUES (?, ?, ?, ?)
        \`);
        
        insertPattern.run('API Response Wrapper', 'backend', 
            'Standardized API response format with status, data, and error fields',
            'backend-architect');
            
        insertPattern.run('Atomic Design Components', 'frontend',
            'Component hierarchy: atoms -> molecules -> organisms -> templates -> pages',
            'frontend-developer');
        
        // Add test components
        const insertComponent = knowledgeDb.prepare(\`
            INSERT OR IGNORE INTO components (component_name, component_type, description, dependencies, created_by)
            VALUES (?, ?, ?, ?, ?)
        \`);
        
        insertComponent.run('BibleSearch', 'ui', 'Advanced Bible search component',
            'react, typescript, bible-service', 'frontend-developer');
            
        insertComponent.run('PrayerService', 'service', 'Prayer generation backend service',
            'postgresql, ai-service', 'backend-architect');
        
        // Add test actions
        const insertAction = auditDb.prepare(\`
            INSERT INTO agent_actions (agent_name, action_type, description, success)
            VALUES (?, ?, ?, ?)
        \`);
        
        insertAction.run('memory-manager', 'system-test', 'Initialized test data for MCP system', true);
        
        console.log('✓ Test data initialized successfully');
        
        knowledgeDb.close();
        auditDb.close();
    "
}

# Main script logic
case "$1" in
    status)
        show_status
        ;;
    backup)
        backup_memory
        ;;
    decisions)
        recent_decisions
        ;;
    activity)
        agent_activity
        ;;
    patterns)
        show_patterns
        ;;
    components)
        show_components
        ;;
    init-test)
        init_test_data
        ;;
    *)
        echo -e "${CYAN}🧠 MCP Memory System Utilities${NC}"
        echo "=============================="
        echo ""
        echo "Usage: $0 {command}"
        echo ""
        echo "Commands:"
        echo "  status      - Show system status and statistics"
        echo "  backup      - Backup all databases"
        echo "  decisions   - Show recent architectural decisions"
        echo "  activity    - Show agent activity summary"
        echo "  patterns    - List all design patterns"
        echo "  components  - Show registered components"
        echo "  init-test   - Initialize test data"
        echo ""
        echo "Examples:"
        echo "  $0 status"
        echo "  $0 decisions"
        echo "  $0 backup"
        ;;
esac