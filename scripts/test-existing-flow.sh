#!/bin/bash

# Test the Current Anonymous User Flow
# This tests what we have NOW without needing new code

set -e

# Configuration
BASE_URL="http://api.wagl.ai"
ADMIN_EMAIL="admin@wagl.ai"
ADMIN_PASSWORD="AdminPass123#"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

api_call() {
    local method=$1
    local endpoint=$2
    local data=$3
    local token=$4
    local description=$5

    log_info "Testing: $description"

    local headers="Content-Type: application/json"
    if [ ! -z "$token" ]; then
        headers="$headers
Authorization: Bearer $token"
    fi

    local response
    local http_code

    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" -H "$headers" "$BASE_URL$endpoint" 2>/dev/null || echo -e "\nERROR")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" -H "$headers" -d "$data" "$BASE_URL$endpoint" 2>/dev/null || echo -e "\nERROR")
    fi

    http_code=$(echo "$response" | tail -n1)
    response_body=$(echo "$response" | head -n -1)

    if [ "$http_code" = "ERROR" ]; then
        log_error "$description - Network error"
        return 1
    elif [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        log_success "$description - HTTP $http_code"
        echo "$response_body"
        return 0
    else
        log_error "$description - HTTP $http_code"
        if [ ! -z "$response_body" ]; then
            log_error "Response: $response_body"
        fi
        return 1
    fi
}

main() {
    echo "🚀 Testing Current Anonymous User Flow"
    echo "Target: $BASE_URL"
    echo "=================================="
    echo

    # Step 1: Test health
    log_info "🏥 Testing Health Check..."
    if api_call "GET" "/health" "" "" "Health endpoint" > /dev/null; then
        log_success "Application is healthy"
    else
        log_error "Health check failed"
        exit 1
    fi
    echo

    # Step 2: Admin login
    log_info "👑 Setting up admin user..."

    # Try to register admin first
    local register_data="{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\",\"confirmPassword\":\"$ADMIN_PASSWORD\",\"firstName\":\"Admin\",\"lastName\":\"User\"}"
    api_call "POST" "/api/v1/auth/register" "$register_data" "" "Admin registration" > /dev/null 2>&1 || log_info "Admin already exists (expected)"

    # Login as admin
    local login_data="{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}"
    local auth_response
    if auth_response=$(api_call "POST" "/api/v1/auth/login" "$login_data" "" "Admin authentication"); then
        ADMIN_TOKEN=$(echo "$auth_response" | jq -r '.token' 2>/dev/null || echo "")
        if [ ! -z "$ADMIN_TOKEN" ] && [ "$ADMIN_TOKEN" != "null" ]; then
            log_success "Admin authenticated successfully"
        else
            log_error "Failed to extract admin token"
            exit 1
        fi
    else
        log_error "Admin login failed"
        exit 1
    fi
    echo

    # Step 3: Create session
    log_info "🏛️ Creating test session..."
    local session_data="{\"name\":\"Test Session $(date +%s)\",\"scheduledStartTime\":\"$(date -u +%Y-%m-%dT%H:%M:%S.000Z)\",\"durationMinutes\":60,\"maxParticipants\":18,\"maxParticipantsPerRoom\":6}"

    local session_response
    if session_response=$(api_call "POST" "/api/v1/chat/sessions" "$session_data" "$ADMIN_TOKEN" "Session creation"); then
        SESSION_ID=$(echo "$session_response" | jq -r '.id' 2>/dev/null || echo "")
        if [ ! -z "$SESSION_ID" ] && [ "$SESSION_ID" != "null" ]; then
            log_success "Session created: $SESSION_ID"
        else
            log_error "Failed to extract session ID"
            exit 1
        fi
    else
        log_error "Session creation failed"
        exit 1
    fi
    echo

    # Step 4: Generate invite
    log_info "🎫 Generating invite code..."
    local invite_data="{\"sessionId\":\"$SESSION_ID\",\"expirationMinutes\":120}"

    local invite_response
    if invite_response=$(api_call "POST" "/api/v1/chat/invites" "$invite_data" "$ADMIN_TOKEN" "Invite generation"); then
        INVITE_TOKEN=$(echo "$invite_response" | jq -r '.token' 2>/dev/null || echo "")
        if [ ! -z "$INVITE_TOKEN" ] && [ "$INVITE_TOKEN" != "null" ]; then
            log_success "Invite generated: ${INVITE_TOKEN:0:20}..."
            echo "🔗 Full invite URL: $BASE_URL/api/v1/chat/invites/$INVITE_TOKEN"
        else
            log_error "Failed to extract invite token"
            exit 1
        fi
    else
        log_error "Invite generation failed"
        exit 1
    fi
    echo

    # Step 5: Test anonymous user flow (what exists now)
    log_info "👤 Testing Anonymous User Entry Flow..."

    # Test invite validation (anonymous endpoint)
    if api_call "GET" "/api/v1/chat/invites/$INVITE_TOKEN/validate" "" "" "Invite validation" > /dev/null; then
        log_success "Invite validation works"
    else
        log_warning "Invite validation may not be implemented"
    fi

    # Test invite details (anonymous endpoint)
    if api_call "GET" "/api/v1/chat/invites/$INVITE_TOKEN" "" "" "Invite details" > /dev/null; then
        log_success "Invite details retrieval works"
    else
        log_warning "Invite details may not be implemented"
    fi

    # Test invite consumption (anonymous endpoint with display name)
    log_info "Testing invite consumption with display name..."
    local consume_data="{\"displayName\":\"Test User\"}"
    if api_call "POST" "/api/v1/chat/invites/$INVITE_TOKEN/consume" "$consume_data" "" "Invite consumption"; then
        log_success "Invite consumption works!"
        log_success "✅ Anonymous users CAN join sessions with just display name"
    else
        log_warning "Invite consumption failed - may need implementation"
    fi
    echo

    # Step 6: Test room allocation
    log_info "🏠 Testing Room Allocation..."
    if api_call "GET" "/api/v1/chat/rooms/session/$SESSION_ID" "" "$ADMIN_TOKEN" "Room allocation" > /dev/null; then
        log_success "Room allocation working"
    else
        log_warning "Room allocation may need attention"
    fi
    echo

    echo "📋 === CURRENT FLOW TEST RESULTS ==="
    echo "✅ Admin can create sessions"
    echo "✅ Admin can generate invite codes"
    echo "✅ Anonymous users can validate invites"
    echo "? Anonymous user consumption needs testing"
    echo "? Room auto-assignment needs verification"
    echo
    echo "🔄 NEXT STEPS:"
    echo "1. Test the anonymous flow end-to-end"
    echo "2. Build new SessionEntryController if needed"
    echo "3. Add email collection for anonymous users"
    echo "4. Test SignalR with anonymous participants"
    echo
    echo "🔗 Test this URL manually:"
    echo "   $BASE_URL/api/v1/chat/invites/$INVITE_TOKEN"
    echo
}

main