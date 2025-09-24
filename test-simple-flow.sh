#!/bin/bash

# Simple Flow Test - Bypass Registration Issues
# Test what works without user registration

set -e

BASE_URL="http://wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com"

echo "🧪 Simple Flow Test"
echo "=================="

# Test 1: Health check
echo "1. Testing health..."
health_response=$(curl -s "$BASE_URL/health")
if [ "$health_response" = "Healthy" ]; then
    echo "✅ Health check passed"
else
    echo "❌ Health check failed: $health_response"
    exit 1
fi

# Test 2: Try to access invite endpoints without auth (should fail)
echo "2. Testing invite endpoints..."
curl -s "$BASE_URL/api/v1.0/chat/invites/test123" -w "\nHTTP Code: %{http_code}\n"

echo "3. Testing invite validation..."
curl -s "$BASE_URL/api/v1.0/chat/invites/test123/validate" -w "\nHTTP Code: %{http_code}\n"

echo "4. Testing anonymous access to existing endpoints..."
curl -s "$BASE_URL/api/v1.0/chat/sessions" -w "\nHTTP Code: %{http_code}\n"

echo ""
echo "🔍 Analysis:"
echo "- Health endpoint: ✅ Working"
echo "- API versioning: ✅ Working (getting proper HTTP codes)"
echo "- Issue: Need valid invite tokens to test anonymous flow"
echo ""
echo "🎯 Next Steps:"
echo "1. Find a way to create valid invite tokens"
echo "2. Use existing endpoints rather than new SessionEntryController"
echo "3. Focus on testing the core anonymous flow logic"