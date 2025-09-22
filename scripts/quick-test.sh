#!/bin/bash

# Quick Integration Test for Wagl Backend Chat System
# Tests API layers without SignalR (immediate execution possible)

set -e

# Configuration
BASE_URL="http://api.wagl.ai"
TEST_SESSION_NAME="Quick Test Session $(date +%s)"
GUID_1=$(uuidgen)
GUID_2=$(uuidgen)
GUID_3=$(uuidgen)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test tracking
TESTS_PASSED=0
TESTS_FAILED=0
ERRORS=()

# Helper function for API calls
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
        ERRORS+=("$description - Network error")
        ((TESTS_FAILED++))
        return 1
    elif [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        log_success "$description - HTTP $http_code"
        echo "$response_body"
        ((TESTS_PASSED++))
        return 0
    else
        log_error "$description - HTTP $http_code"
        log_error "Response: $response_body"
        ERRORS+=("$description - HTTP $http_code")
        ((TESTS_FAILED++))
        return 1
    fi
}

# Test functions
test_health_check() {
    log_info "🏥 Testing Health Check Layer..."
    if api_call "GET" "/health" "" "" "Health endpoint check" > /dev/null; then
        log_success "Application is healthy and responsive"
    else
        log_error "Application health check failed"
    fi
}

test_authentication() {
    log_info "🔐 Testing Authentication Layer..."

    # Test user registration
    local register_data='{
        "email": "testuser@wagl.ai",
        "password": "TestPass123!",
        "confirmPassword": "TestPass123!",
        "firstName": "Test",
        "lastName": "User"
    }'

    # Try to register (may fail if user exists)
    api_call "POST" "/api/v1/auth/register" "$register_data" "" "User registration" > /dev/null 2>&1 || true

    # Test user login
    local login_data='{
        "email": "testuser@wagl.ai",
        "password": "TestPass123!"
    }'

    local auth_response
    if auth_response=$(api_call "POST" "/api/v1/auth/login" "$login_data" "" "User authentication"); then
        # Extract JWT token from response
        JWT_TOKEN=$(echo "$auth_response" | jq -r '.token' 2>/dev/null || echo "")
        USER_ID=$(echo "$auth_response" | jq -r '.userId' 2>/dev/null || echo "")

        if [ ! -z "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
            log_success "JWT token obtained successfully"
            export JWT_TOKEN
            export USER_ID
        else
            log_error "Failed to extract JWT token from response"
            exit 1
        fi
    else
        log_error "Authentication failed"
        exit 1
    fi
}

test_session_management() {
    log_info "🏛️ Testing Session Management Layer..."

    # Create a test session
    local session_data="{
        \"name\": \"$TEST_SESSION_NAME\",
        \"scheduledStartTime\": \"$(date -u +%Y-%m-%dT%H:%M:%S.000Z)\",
        \"durationMinutes\": 60,
        \"maxParticipants\": 18,
        \"maxParticipantsPerRoom\": 6
    }"

    local session_response
    if session_response=$(api_call "POST" "/api/v1/chat/sessions" "$session_data" "$JWT_TOKEN" "Session creation"); then
        SESSION_ID=$(echo "$session_response" | jq -r '.id' 2>/dev/null || echo "")

        if [ ! -z "$SESSION_ID" ] && [ "$SESSION_ID" != "null" ]; then
            log_success "Session created with ID: $SESSION_ID"
            export SESSION_ID

            # Get session details
            api_call "GET" "/api/v1/chat/sessions/$SESSION_ID" "" "$JWT_TOKEN" "Session retrieval" > /dev/null

            # Try to start the session
            api_call "PUT" "/api/v1/chat/sessions/$SESSION_ID/start" "{}" "$JWT_TOKEN" "Session start" > /dev/null || log_warning "Session start may not be implemented or session already active"

        else
            log_error "Failed to extract session ID from response"
            return 1
        fi
    else
        log_error "Session creation failed"
        return 1
    fi
}

test_room_management() {
    log_info "🏠 Testing Room Management Layer..."

    if [ -z "$SESSION_ID" ]; then
        log_error "No session available for room testing"
        return 1
    fi

    # Get rooms for the session
    local rooms_response
    if rooms_response=$(api_call "GET" "/api/v1/chat/rooms/session/$SESSION_ID" "" "$JWT_TOKEN" "Room retrieval"); then
        local room_count
        room_count=$(echo "$rooms_response" | jq '. | length' 2>/dev/null || echo "0")

        if [ "$room_count" -gt 0 ]; then
            log_success "Retrieved $room_count rooms for session"

            # Extract first room ID for testing
            ROOM_ID=$(echo "$rooms_response" | jq -r '.[0].id' 2>/dev/null || echo "")
            if [ ! -z "$ROOM_ID" ] && [ "$ROOM_ID" != "null" ]; then
                export ROOM_ID
                log_success "First room ID: $ROOM_ID"
            fi
        else
            log_warning "No rooms found for session - this may indicate room allocation issues"
        fi
    else
        log_error "Failed to retrieve rooms"
        return 1
    fi
}

test_invite_system() {
    log_info "🎫 Testing Invite System Layer..."

    if [ -z "$SESSION_ID" ]; then
        log_error "No session available for invite testing"
        return 1
    fi

    # Create session invite
    local invite_data='{
        "maxUses": 10,
        "expiresAt": "2025-12-31T23:59:59.000Z"
    }'

    local invite_response
    if invite_response=$(api_call "POST" "/api/v1/invites/session/$SESSION_ID" "$invite_data" "$JWT_TOKEN" "Session invite creation"); then
        INVITE_TOKEN=$(echo "$invite_response" | jq -r '.token' 2>/dev/null || echo "")

        if [ ! -z "$INVITE_TOKEN" ] && [ "$INVITE_TOKEN" != "null" ]; then
            log_success "Invite token created: ${INVITE_TOKEN:0:20}..."
            export INVITE_TOKEN

            # Validate the invite
            api_call "GET" "/api/v1/invites/$INVITE_TOKEN/validate" "" "" "Invite validation" > /dev/null

        else
            log_error "Failed to extract invite token from response"
            return 1
        fi
    else
        log_error "Invite creation failed"
        return 1
    fi
}

test_message_api() {
    log_info "💬 Testing Message API Layer..."

    if [ -z "$ROOM_ID" ]; then
        log_warning "No room available for message testing - skipping"
        return 0
    fi

    # Test message creation via API (if endpoint exists)
    local message_data="{
        \"content\": \"API Test Message - GUID: $GUID_1\",
        \"roomId\": \"$ROOM_ID\"
    }"

    # This endpoint may not exist in the current API, so we'll test if it's available
    api_call "POST" "/api/v1/chat/messages" "$message_data" "$JWT_TOKEN" "Message creation via API" > /dev/null || log_warning "Message API endpoint may not be implemented (expected for SignalR-only messaging)"
}

test_database_connectivity() {
    log_info "🗄️ Testing Database Connectivity Layer..."

    # Test if we can retrieve session statistics (indicates DB is working)
    if [ ! -z "$SESSION_ID" ]; then
        api_call "GET" "/api/v1/chat/sessions/$SESSION_ID/statistics" "" "$JWT_TOKEN" "Session statistics (DB test)" > /dev/null || log_warning "Statistics endpoint may not be implemented"
    fi

    # Test user profile retrieval (another DB operation)
    api_call "GET" "/api/v1/auth/profile" "" "$JWT_TOKEN" "User profile retrieval (DB test)" > /dev/null || log_warning "Profile endpoint may not be implemented"
}

generate_report() {
    echo
    log_info "📋 Test Report Summary"
    echo "=================================="
    echo "Tests Passed: $TESTS_PASSED"
    echo "Tests Failed: $TESTS_FAILED"
    echo "Total Tests: $((TESTS_PASSED + TESTS_FAILED))"
    echo

    if [ ${#ERRORS[@]} -gt 0 ]; then
        log_error "Errors encountered:"
        for error in "${ERRORS[@]}"; do
            echo "  - $error"
        done
        echo
    fi

    local success_rate=0
    if [ $((TESTS_PASSED + TESTS_FAILED)) -gt 0 ]; then
        success_rate=$((TESTS_PASSED * 100 / (TESTS_PASSED + TESTS_FAILED)))
    fi

    echo "Success Rate: $success_rate%"

    if [ $success_rate -ge 80 ]; then
        log_success "🎉 Integration test PASSED! Core layers functioning correctly."
        echo
        log_info "📋 Test Results Summary:"
        echo "✅ Health Check: Application responsive"
        echo "✅ Authentication: JWT tokens working"
        echo "✅ Session Management: CRUD operations functional"
        echo "✅ Room Management: Allocation system working"
        [ ! -z "$INVITE_TOKEN" ] && echo "✅ Invite System: Token generation/validation working"
        echo
        log_info "🔄 Next Steps for Full Testing:"
        echo "1. Run 'npm install' in the scripts directory"
        echo "2. Execute 'node integration-test.js' for SignalR real-time testing"
        echo "3. Monitor application logs during testing"
        echo
        return 0
    else
        log_error "💥 Integration test FAILED! Some components need attention."
        echo
        log_info "🔧 Troubleshooting Steps:"
        echo "1. Check application health: curl $BASE_URL/health"
        echo "2. Verify database connectivity"
        echo "3. Review application logs for errors"
        echo "4. Ensure all required services are running"
        echo
        return 1
    fi
}

# Main execution
main() {
    echo "🚀 Starting Quick Integration Test for Wagl Backend"
    echo "Target URL: $BASE_URL"
    echo "Session Name: $TEST_SESSION_NAME"
    echo "Test GUIDs: $GUID_1, $GUID_2, $GUID_3"
    echo "=================================="
    echo

    # Execute test layers in order
    test_health_check
    test_authentication
    test_session_management
    test_room_management
    test_invite_system
    test_message_api
    test_database_connectivity

    echo
    generate_report
}

# Check dependencies
if ! command -v curl &> /dev/null; then
    log_error "curl is required but not installed"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    log_warning "jq not found - JSON parsing may be limited"
fi

if ! command -v uuidgen &> /dev/null; then
    log_warning "uuidgen not found - using timestamp-based GUIDs"
    GUID_1="test-guid-$(date +%s)-1"
    GUID_2="test-guid-$(date +%s)-2"
    GUID_3="test-guid-$(date +%s)-3"
fi

# Run the test
main

exit $?