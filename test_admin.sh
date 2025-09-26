#!/bin/bash

echo "🔍 Testing Admin Functionality for bash@sentry10.com"
echo "=================================================="

# Login and get token
echo "1. Testing login..."
LOGIN_RESPONSE=$(curl -s -X POST "https://api.wagl.ai/api/v1.0/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "bash@sentry10.com", "password": "$Cellboat2580"}')

if echo "$LOGIN_RESPONSE" | grep -q "accessToken"; then
    echo "✅ Login successful"
    TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin)['accessToken'])")

    # Decode JWT to check role
    ROLE=$(echo "$TOKEN" | python3 -c "
import sys, json, base64
token = sys.stdin.read().strip()
payload = token.split('.')[1]
# Add padding if needed
payload += '=' * (4 - len(payload) % 4)
decoded = json.loads(base64.b64decode(payload))
print('Primary Role:', decoded.get('role', 'Unknown'))
print('All role claims:', [v for k, v in decoded.items() if k == 'role'])
print('User info:', {k: v for k, v in decoded.items() if k in ['sub', 'email', 'name']})
print('Auth checks needed for ChatAdmin policy:')
print('- Has Provider role:', 'Provider' in [v for k, v in decoded.items() if k == 'role'])
print('- Has ChatAdmin role:', 'ChatAdmin' in [v for k, v in decoded.items() if k == 'role'])
print('- Has Admin role:', 'Admin' in [v for k, v in decoded.items() if k == 'role'])
")
    echo "🔑 JWT Token Analysis:"
    echo "$ROLE"
else
    echo "❌ Login failed: $LOGIN_RESPONSE"
    exit 1
fi

echo ""
echo "2. Testing session creation..."
SESSION_RESPONSE=$(curl -s -X POST "https://api.wagl.ai/api/v1.0/chat/sessions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "sessionName": "Test Admin Session",
    "description": "Testing admin access"
  }')

if echo "$SESSION_RESPONSE" | grep -q "sessionId\|id"; then
    echo "✅ Session creation successful"
    echo "📝 Session response: $SESSION_RESPONSE"
else
    echo "❌ Session creation failed"
    echo "📝 Error response: $SESSION_RESPONSE"
fi

echo ""
echo "3. Testing admin endpoints..."

# Test user management endpoint
echo "3a. Testing user list endpoint..."
USER_LIST_RESPONSE=$(curl -s -w "HTTP_STATUS:%{http_code}" "https://api.wagl.ai/api/v1.0/user/users" \
  -H "Authorization: Bearer $TOKEN")

HTTP_STATUS=$(echo "$USER_LIST_RESPONSE" | grep -o "HTTP_STATUS:[0-9]*" | cut -d: -f2)
RESPONSE_BODY=$(echo "$USER_LIST_RESPONSE" | sed 's/HTTP_STATUS:[0-9]*$//')

echo "📊 HTTP Status: $HTTP_STATUS"
if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ User list access successful"
    echo "📝 Response: $RESPONSE_BODY"
elif [ "$HTTP_STATUS" = "401" ]; then
    echo "❌ Unauthorized - Admin role required but not present"
elif [ "$HTTP_STATUS" = "404" ]; then
    echo "⚠️  Endpoint not found - may not be implemented"
else
    echo "❌ Unexpected status: $HTTP_STATUS"
    echo "📝 Response: $RESPONSE_BODY"
fi

echo ""
echo "4. Summary:"
echo "- Login: ✅ Working"
echo "- Role in JWT: $ROLE"
echo "- Session creation: $(if echo "$SESSION_RESPONSE" | grep -q "sessionId\|id"; then echo "✅ Working"; else echo "❌ Failed"; fi)"
echo "- Admin endpoints: Status $HTTP_STATUS"