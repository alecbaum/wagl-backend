# Backend Features TODO

This file tracks backend features that may be missing for comprehensive front-end testing.

## CRITICAL: Missing Basic API Endpoints (Causing 404 Errors in Frontend)

**These endpoints are documented in api-v1.md but not implemented in the backend:**

### Session Management (Admin Only)
- [ ] POST `/api/v1.0/chat/sessions` - Create new chat session
  - Requires admin JWT authentication
  - Request body: `{ "sessionName": "string", "sessionDescription": "string", "isPrivate": boolean }`
  - Response: Session object with ID and details
- [ ] GET `/api/v1.0/chat/sessions/my-sessions` - Get current user's sessions
  - Returns array of sessions the authenticated user has access to
- [ ] POST `/api/v1.0/chat/sessions/{sessionId}/start` - Start a scheduled session
  - Changes session status from scheduled to active
- [ ] POST `/api/v1.0/chat/sessions/{sessionId}/end` - End an active session
  - Changes session status from active to completed

### Invite Management (Admin Only)
- [ ] POST `/api/v1.0/chat/invites` - Generate session invite code
  - Requires admin JWT authentication
  - Request body: `{ "sessionId": "string", "expiresIn": number, "maxUses": number }`
  - Response: `{ "inviteCode": "string", "expiresAt": "datetime", "maxUses": number }`
- [ ] GET `/api/v1.0/chat/invites/session/{sessionId}` - Get all invites for a session
  - Returns array of invite objects with usage stats

### Dashboard Statistics
- [ ] GET `/api/v1.0/dashboard/stats` - Basic dashboard statistics
  - Returns: `{ "totalSessions": number, "activeSessions": number, "totalUsers": number, "activeUsers": number }`
  - Should be accessible to authenticated users

### Provider Management (API Key Authentication)
- [ ] POST `/api/v1.0/providers` - Create new provider account (Admin only)
  - Request body: `{ "providerName": "string", "email": "string", "allowedIpAddresses": ["string"] }`
  - Response: `{ "providerId": "string", "apiKey": "string" }`
- [ ] GET `/api/v1.0/providers/my-profile` - Get current provider profile
  - For providers authenticated with API key
- [ ] PUT `/api/v1.0/providers/regenerate-api-key` - Regenerate API key
  - Returns new API key, invalidates old one

### Admin Permission Verification
- [ ] Implement proper admin role checking for session/invite/provider creation
- [ ] Add `[Authorize(Policy = "AdminOnly")]` or equivalent to admin endpoints
- [ ] Ensure regular users get 403 Forbidden (not 404) when accessing admin endpoints

## Missing API Endpoints (Identified during front-end demo development)

### Dashboard/Statistics Endpoints
- [ ] GET `/api/v1.0/admin/dashboard/stats` - Overall system statistics
  - Total sessions (active, scheduled, completed)
  - Total rooms across all sessions
  - Total users (registered)
  - Total active participants
  - Current system load metrics

### User Management
- [ ] GET `/api/v1.0/admin/users` - List all users with pagination
- [ ] GET `/api/v1.0/admin/users/{userId}` - Get specific user details
- [ ] PUT `/api/v1.0/admin/users/{userId}/tier` - Change user tier level
- [ ] GET `/api/v1.0/admin/users/stats` - User statistics by tier

### Advanced Session Management
- [ ] PUT `/api/v1.0/chat/sessions/{sessionId}/status` - Change session status (pause/resume)
- [ ] GET `/api/v1.0/chat/sessions/{sessionId}/participants` - List all session participants
- [ ] DELETE `/api/v1.0/chat/sessions/{sessionId}/participants/{participantId}` - Remove participant

### Room Administration
- [ ] POST `/api/v1.0/chat/rooms/{roomId}/close` - Manually close a room
- [ ] POST `/api/v1.0/chat/rooms/{roomId}/open` - Reopen a closed room
- [ ] DELETE `/api/v1.0/chat/rooms/{roomId}/participants/{participantId}` - Remove participant from room

### Moderator Features
- [ ] POST `/api/v1.0/chat/rooms/{roomId}/messages/moderator` - Send moderator message
- [ ] PUT `/api/v1.0/chat/messages/{messageId}/moderate` - Hide/delete inappropriate messages
- [ ] GET `/api/v1.0/admin/moderation/queue` - Get messages pending moderation

### Bot Integration
- [ ] POST `/api/v1.0/chat/rooms/{roomId}/messages/bot` - Send bot message manually
- [ ] GET `/api/v1.0/admin/bots` - List available bots
- [ ] PUT `/api/v1.0/chat/sessions/{sessionId}/bots/{botId}` - Enable/disable bot for session

### Enhanced Analytics
- [ ] GET `/api/v1.0/analytics/sessions/{sessionId}/activity` - Session activity timeline
- [ ] GET `/api/v1.0/analytics/rooms/{roomId}/metrics` - Room engagement metrics
- [ ] GET `/api/v1.0/analytics/users/{userId}/activity` - User activity history

### Real-time Admin Controls
- [ ] SignalR Hub method: `BroadcastSystemMessage(message)` - System-wide announcements
- [ ] SignalR Hub method: `ModerateMessage(messageId, action)` - Real-time message moderation
- [ ] SignalR Hub event: `SystemMessageReceived` - For system announcements
- [ ] SignalR Hub event: `MessageModerated` - Notify when message is moderated

### Authentication Enhancements
- [ ] POST `/api/v1.0/auth/admin-login` - Admin-specific login with additional permissions
- [ ] GET `/api/v1.0/auth/permissions` - Get current user permissions
- [ ] POST `/api/v1.0/auth/impersonate/{userId}` - Admin impersonation for testing

## UI Testing Requirements

### Multi-User Testing
- [ ] Need ability to simulate multiple users simultaneously
- [ ] Test user interactions across different rooms
- [ ] Verify message isolation between rooms
- [ ] Test concurrent session participation

### Error Handling
- [ ] Test rate limiting behavior
- [ ] Test invalid token scenarios
- [ ] Test network disconnection recovery
- [ ] Test room capacity limits

### Performance Testing
- [ ] Load testing with multiple participants
- [ ] Message throughput testing
- [ ] SignalR connection stability under load
- [ ] Memory usage monitoring

## Implementation Priority

**High Priority** (Core missing features):
1. Dashboard statistics endpoints
2. Advanced session participant management
3. Room administration controls
4. Enhanced user management

**Medium Priority** (Nice to have):
1. Moderator message features
2. Bot integration endpoints
3. Analytics and reporting

**Low Priority** (Advanced features):
1. Admin impersonation
2. System-wide messaging
3. Advanced moderation queue

## Notes

- All endpoints should follow existing API patterns (v1.0, consistent error handling)
- Authentication requirements should match existing endpoints
- Rate limiting should apply to new endpoints
- SignalR events should follow existing naming conventions
- Consider pagination for list endpoints
- Include proper OpenAPI documentation for new endpoints

## Analysis Summary (Based on API Spec vs Frontend Test Results)

**Key Findings from Deep Dive Error Analysis:**
- Frontend tests confirm authentication works properly (registration and login successful)
- Session creation, invite generation, and moderator/provider creation all fail with 404 errors
- These failures occur because the basic CRUD endpoints documented in `api-v1.md` are not implemented in the backend
- Admin permission enforcement is also missing - regular users should get 403 Forbidden, not 404
- The frontend correctly calls the endpoints as documented in the API specification

**Root Cause:**
The backend is missing fundamental session and invite management endpoints that are required for the application to function as designed. While authentication infrastructure exists, the core business logic endpoints are not yet implemented.

**Priority:**
Implementing the "CRITICAL" section endpoints above will resolve all major functionality issues discovered during frontend testing.

---
*This file is maintained during front-end demo development and updated based on API specification analysis. Do not implement these features while working on the demo - only document them here.*