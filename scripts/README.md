# Wagl Backend Integration Testing Suite

This directory contains comprehensive integration tests for the Wagl Backend chat system. The tests validate all layers of the application stack to ensure proper functionality in real-world scenarios.

## 🎯 Test Objectives

The integration tests are designed to validate:

1. **Authentication Layer** - JWT token generation and validation
2. **Session Management** - Creating, starting, and managing chat sessions
3. **Room Allocation** - Automatic room creation and capacity management
4. **Real-time Communication** - SignalR connections and message routing
5. **Message Persistence** - Database storage and retrieval
6. **Message Isolation** - Ensuring messages stay within correct rooms
7. **Background Services** - Session lifecycle and cleanup operations

## 📋 Test Scenarios

### Quick Test (Bash Script)
- **File**: `quick-test.sh`
- **Purpose**: Fast API layer validation without SignalR
- **Duration**: ~30 seconds
- **Requirements**: curl, jq (optional), uuidgen (optional)

### Comprehensive Test (Node.js)
- **File**: `integration-test.js`
- **Purpose**: Full end-to-end testing including real-time messaging
- **Duration**: ~5-10 minutes
- **Requirements**: Node.js 18+, npm

## 🚀 Quick Start

### Option 1: Quick API Test (Immediate)

```bash
# Make the script executable
chmod +x quick-test.sh

# Run the quick test
./quick-test.sh
```

This will test:
- Application health check
- User authentication (JWT tokens)
- Session creation and management
- Room allocation
- Invite system
- Basic API connectivity

### Option 2: Full Integration Test

```bash
# Install dependencies
npm install

# Run comprehensive test
npm test
# or
node integration-test.js
```

This will test everything in Option 1 plus:
- SignalR real-time connections
- Multiple user simulation (6 users per room)
- Message routing and delivery
- Cross-room message isolation
- Database persistence verification

## 📊 Test Configuration

### Quick Test Configuration
- **Base URL**: `http://api.wagl.ai`
- **Test User**: `testuser@wagl.ai`
- **Session Duration**: 60 minutes
- **Room Capacity**: 6 users per room

### Comprehensive Test Configuration
```javascript
const config = {
    baseUrl: 'http://api.wagl.ai',
    hubUrl: 'http://api.wagl.ai/chathub',
    testUsers: 6,           // Total test users
    roomsToTest: 3,         // Number of rooms to create
    usersPerRoom: 6,        // Users per room
    messagesPerUser: 3      // Messages each user sends
};
```

## 📈 Expected Test Results

### Successful Test Output

#### Quick Test:
```
🚀 Starting Quick Integration Test for Wagl Backend
==================================

[INFO] 🏥 Testing Health Check Layer...
[SUCCESS] Health endpoint check - HTTP 200
[INFO] 🔐 Testing Authentication Layer...
[SUCCESS] User authentication - HTTP 200
[SUCCESS] JWT token obtained successfully
[INFO] 🏛️ Testing Session Management Layer...
[SUCCESS] Session creation - HTTP 201
[SUCCESS] Session created with ID: sess_123...

📋 Test Report Summary
==================================
Tests Passed: 8
Tests Failed: 0
Total Tests: 8
Success Rate: 100%
🎉 Integration test PASSED! Core layers functioning correctly.
```

#### Comprehensive Test:
```
[INFO] 🔐 Testing Authentication Layer...
[INFO] ✅ User Alice authenticated successfully
[INFO] ✅ User Bob authenticated successfully
...
[INFO] 🏛️ Testing Session Management Layer...
[INFO] ✅ Session created: Integration Test Session (ID: sess_123)
[INFO] 🏠 Testing Room Allocation Layer...
[INFO] ✅ Retrieved 3 rooms for session
[INFO] ⚡ Testing SignalR Real-time Layer...
[INFO] ✅ Alice connected to SignalR hub
[INFO] ✅ Alice joined room room_abc
...
[INFO] 💬 Testing Message Routing and Persistence Layer...
[INFO] 📤 Alice sent message to room room_abc: "Test message 1 from Alice - GUID: 123e4567-e89b-12d3-a456-426614174000"
[INFO] 📨 Bob received message in room room_abc: "Test message 1 from Alice - GUID: 123e4567-e89b-12d3-a456-426614174000"
...
🎯 Overall Test Success Rate: 95.2%
🎉 Integration test PASSED! All layers functioning correctly.
```

## 🔍 What Each Test Validates

### 1. Health Check Layer
- **Validates**: Application responsiveness
- **Tests**: `GET /health`
- **Expected**: HTTP 200 with "Healthy" response

### 2. Authentication Layer
- **Validates**: User registration and JWT token generation
- **Tests**: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`
- **Expected**: Valid JWT token in response

### 3. Session Management Layer
- **Validates**: CRUD operations for chat sessions
- **Tests**:
  - Session creation: `POST /api/v1/chat/sessions`
  - Session retrieval: `GET /api/v1/chat/sessions/{id}`
  - Session start: `PUT /api/v1/chat/sessions/{id}/start`
- **Expected**: Session objects with proper status transitions

### 4. Room Allocation Layer
- **Validates**: Automatic room creation based on session parameters
- **Tests**: `GET /api/v1/chat/rooms/session/{sessionId}`
- **Expected**: Rooms created according to maxParticipants/maxParticipantsPerRoom

### 5. Invite System Layer
- **Validates**: Invite token generation and validation
- **Tests**:
  - Create invite: `POST /api/v1/invites/session/{sessionId}`
  - Validate invite: `GET /api/v1/invites/{token}/validate`
- **Expected**: Working invite tokens for room access

### 6. SignalR Real-time Layer (Comprehensive Test Only)
- **Validates**: WebSocket connections and real-time communication
- **Tests**:
  - Hub connection to `/chathub`
  - `JoinRoomWithToken` method
  - `SendMessage` method
  - Message event reception
- **Expected**: Bidirectional real-time communication

### 7. Message Routing and Persistence
- **Validates**: Messages are delivered to correct recipients and stored
- **Tests**:
  - Send messages with unique GUIDs
  - Verify delivery to room members only
  - Check message persistence
- **Expected**: 100% message delivery within rooms, 0% cross-room leakage

### 8. Database Connectivity
- **Validates**: Proper data persistence and retrieval
- **Tests**: Various API endpoints that require database operations
- **Expected**: Successful data operations without errors

## 🛠️ Troubleshooting

### Common Issues

#### 1. Connection Refused
```
[ERROR] Health endpoint check - Network error
```
**Solution**: Ensure the application is running and accessible at the configured URL

#### 2. Authentication Failures
```
[ERROR] User authentication - HTTP 401
```
**Solutions**:
- Check if user registration is working
- Verify password requirements
- Ensure authentication endpoints are properly configured

#### 3. SignalR Connection Issues (Comprehensive Test)
```
[ERROR] Failed to connect Alice to SignalR: Connection timeout
```
**Solutions**:
- Verify SignalR hub is properly configured
- Check if WebSocket connections are allowed
- Ensure Redis backplane is working for SignalR

#### 4. Message Delivery Issues
```
Messages sent: 18, Messages received: 12
```
**Solutions**:
- Check SignalR Redis backplane connectivity
- Verify room group management in SignalR hub
- Review background service logs for errors

### Debugging Steps

1. **Check Application Health**:
   ```bash
   curl http://api.wagl.ai/health
   ```

2. **Review Application Logs**:
   ```bash
   # Check recent ECS logs
   aws logs get-log-events --log-group-name /ecs/wagl-backend --log-stream-name [task-id] --start-time $(date -d '10 minutes ago' +%s)000
   ```

3. **Test Individual Components**:
   ```bash
   # Test authentication
   curl -X POST http://api.wagl.ai/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@wagl.ai","password":"TestPass123!"}'
   ```

4. **Monitor Database Connectivity**:
   - Check if sessions are being created in the database
   - Verify room allocation is working
   - Ensure message persistence is functioning

## 📝 Test Customization

### Modifying Test Parameters

#### Quick Test:
Edit variables at the top of `quick-test.sh`:
```bash
BASE_URL="http://localhost:5000"  # Change target URL
TEST_SESSION_NAME="Custom Test"   # Change session name
```

#### Comprehensive Test:
Edit the config object in `integration-test.js`:
```javascript
const config = {
    baseUrl: 'http://localhost:5000',
    testUsers: 12,          // Test with more users
    roomsToTest: 5,         // Test more rooms
    messagesPerUser: 5      // Send more messages
};
```

### Adding Custom Test Users

In `integration-test.js`, modify the `testUsers` array:
```javascript
testUsers: [
    { email: 'alice@test.com', password: 'Password123!', displayName: 'Alice' },
    { email: 'bob@test.com', password: 'Password123!', displayName: 'Bob' },
    // Add more users...
]
```

## 🎯 Success Criteria

### Quick Test Success Criteria:
- ✅ Health check returns 200 OK
- ✅ User authentication succeeds
- ✅ Session creation returns valid session ID
- ✅ Room allocation creates expected number of rooms
- ✅ Invite system generates valid tokens
- ✅ Success rate ≥ 80%

### Comprehensive Test Success Criteria:
- ✅ All users authenticate successfully
- ✅ Session created and started
- ✅ All users connect to SignalR hub
- ✅ All users join their assigned rooms
- ✅ All messages sent and received
- ✅ No cross-room message leakage
- ✅ Success rate ≥ 90%

## 📊 Performance Benchmarks

### Expected Performance:
- **Authentication**: < 500ms per user
- **Session Creation**: < 1 second
- **SignalR Connection**: < 2 seconds per user
- **Message Delivery**: < 100ms per message
- **Overall Test Duration**: 5-10 minutes for comprehensive test

### Performance Red Flags:
- Authentication taking > 2 seconds
- SignalR connections failing frequently
- Message delivery rate < 95%
- Database operations timing out

## 🔄 Continuous Integration

### Running in CI/CD:
```bash
# In your CI pipeline
cd scripts
npm install
npm test

# Check exit code
if [ $? -eq 0 ]; then
    echo "Integration tests passed"
else
    echo "Integration tests failed"
    exit 1
fi
```

### Automated Monitoring:
- Run quick test every hour in production
- Run comprehensive test after deployments
- Alert on success rate < 90%
- Monitor performance trends over time

## 📞 Support

If tests consistently fail or you encounter issues:

1. **Check Recent Changes**: Review recent deployments or configuration changes
2. **Review Logs**: Check application, database, and infrastructure logs
3. **Verify Dependencies**: Ensure all external services (Redis, PostgreSQL) are healthy
4. **Test Environment**: Verify test environment matches expected configuration

## 🔄 Version History

- **v1.0.0**: Initial release with basic API testing
- **v1.1.0**: Added SignalR real-time testing
- **v1.2.0**: Enhanced message isolation testing
- **v1.3.0**: Added comprehensive error reporting and debugging