# Wagl Backend API v1.0 Documentation

## Overview

The Wagl Backend API provides a comprehensive chat session management system with anonymous user support, real-time messaging via SignalR, and tiered authentication. The API follows RESTful principles and uses JWT authentication for registered users and anonymous access for invited participants.

**Base URL**: `http://wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com`
**API Version**: v1.0
**Protocol**: HTTP/HTTPS
**Content-Type**: `application/json`

---

## Authentication

### User Authentication (JWT)
- **Type**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer <jwt_token>`
- **Scope**: Registered users with tier-based access (Tier1, Tier2, Tier3)

### Anonymous Authentication
- **Type**: Invite Token
- **Scope**: Anonymous users accessing via invite links
- **No authentication header required for anonymous endpoints**

---

## Core API Endpoints

### 1. Authentication Endpoints

#### Login
```http
POST /api/v1.0/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "rememberMe": false
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_refresh_token",
  "expiresAt": "2025-09-23T20:51:49.9565743Z",
  "tokenType": "Bearer",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "tierLevel": 1,
    "availableFeatures": ["BasicAPI", "StandardSupport"],
    "hourlyRateLimit": 100,
    "isActive": true,
    "createdAt": "2025-09-23T19:31:59.323102Z",
    "lastLoginAt": "2025-09-23T19:51:49.8585971Z",
    "subscriptionExpiresAt": null
  }
}
```

#### Register
```http
POST /api/v1.0/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "requestedTier": "Tier1"
}
```

**Password Requirements:**
- Minimum 8 characters

#### Refresh Token
```http
POST /api/v1.0/auth/refresh
Content-Type: application/json
Authorization: Bearer <jwt_token>

{
  "refreshToken": "base64_refresh_token"
}
```

#### Logout
```http
POST /api/v1.0/auth/logout
Authorization: Bearer <jwt_token>
```

---

### 2. Chat Session Management

#### Create Session
```http
POST /api/v1.0/chat/sessions
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "name": "My Chat Session",
  "scheduledStartTime": "2025-09-23T20:00:00Z",
  "durationMinutes": 60,
  "maxParticipants": 36,
  "maxParticipantsPerRoom": 6
}
```

**Validation Rules:**
- `name`: 1-100 characters
- `durationMinutes`: 1-1440 (1 minute to 24 hours)
- `maxParticipants`: 6-36 (minimum 1 room, maximum 6 rooms)
- `maxParticipantsPerRoom`: 2-6

**Response:**
```json
{
  "id": "session_uuid",
  "name": "My Chat Session",
  "status": "Scheduled",
  "scheduledStartTime": "2025-09-23T20:00:00Z",
  "durationMinutes": 60,
  "maxParticipants": 36,
  "maxParticipantsPerRoom": 6,
  "currentParticipants": 0,
  "createdAt": "2025-09-23T19:30:00Z",
  "createdBy": "user_uuid",
  "canStart": true,
  "isActive": false,
  "isExpired": false
}
```

#### Start Session
```http
POST /api/v1.0/chat/sessions/{sessionId}/start
Authorization: Bearer <jwt_token>
```

#### End Session
```http
POST /api/v1.0/chat/sessions/{sessionId}/end
Authorization: Bearer <jwt_token>
```

#### Get Session Details
```http
GET /api/v1.0/chat/sessions/{sessionId}
Authorization: Bearer <jwt_token>
```

#### Get Session Status
```http
GET /api/v1.0/chat/sessions/{sessionId}/status
Authorization: Bearer <jwt_token>
```

#### List Active Sessions
```http
GET /api/v1.0/chat/sessions/active
Authorization: Bearer <jwt_token>
```

#### List Scheduled Sessions
```http
GET /api/v1.0/chat/sessions/scheduled
Authorization: Bearer <jwt_token>
```

#### List My Sessions
```http
GET /api/v1.0/chat/sessions/my-sessions
Authorization: Bearer <jwt_token>
```

#### Delete Session
```http
DELETE /api/v1.0/chat/sessions/{sessionId}
Authorization: Bearer <jwt_token>
```

---

### 3. Invite Management

#### Generate Invite
```http
POST /api/v1.0/chat/invites
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "sessionId": "session_uuid",
  "expirationMinutes": 120
}
```

**Response:**
```json
{
  "token": "32_char_base64_invite_token",
  "sessionId": "session_uuid",
  "expiresAt": "2025-09-23T22:00:00Z",
  "inviteUrl": "https://app.wagl.ai/join?code=32_char_base64_invite_token",
  "isActive": true,
  "createdAt": "2025-09-23T20:00:00Z"
}
```

#### Generate Bulk Invites
```http
POST /api/v1.0/chat/invites/bulk
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "sessionId": "session_uuid",
  "recipients": [
    {"email": "user1@example.com", "displayName": "User 1"},
    {"email": "user2@example.com", "displayName": "User 2"}
  ],
  "expirationMinutes": 120
}
```

#### Validate Invite (Anonymous)
```http
GET /api/v1.0/chat/invites/{token}/validate
```

**Response:**
```json
{
  "isValid": true,
  "sessionName": "My Chat Session",
  "expiresAt": "2025-09-23T22:00:00Z",
  "maxParticipants": 36,
  "currentParticipants": 15,
  "canJoin": true
}
```

#### Get Invite Details (Anonymous)
```http
GET /api/v1.0/chat/invites/{token}
```

#### Consume Invite (Anonymous)
```http
POST /api/v1.0/chat/invites/{token}/consume
Content-Type: application/json

{
  "displayName": "Anonymous User"
}
```

**Response:**
```json
{
  "success": true,
  "participantId": "participant_uuid",
  "roomId": "room_uuid",
  "sessionId": "session_uuid",
  "signalRConnectionToken": "signalr_token",
  "participant": {
    "id": "participant_uuid",
    "displayName": "Anonymous User",
    "joinedAt": "2025-09-23T20:15:00Z",
    "isAnonymous": true
  },
  "room": {
    "id": "room_uuid",
    "name": "Room 1",
    "currentParticipants": 4,
    "maxParticipants": 6,
    "canJoin": true
  }
}
```

#### List Session Invites
```http
GET /api/v1.0/chat/invites/session/{sessionId}
Authorization: Bearer <jwt_token>
```

#### List Active Session Invites
```http
GET /api/v1.0/chat/invites/session/{sessionId}/active
Authorization: Bearer <jwt_token>
```

#### Expire Invite
```http
DELETE /api/v1.0/chat/invites/{token}
Authorization: Bearer <jwt_token>
```

#### Get Invite Statistics
```http
GET /api/v1.0/chat/invites/session/{sessionId}/statistics
Authorization: Bearer <jwt_token>
```

---

### 4. Anonymous Session Entry

#### Access Session via Invite URL
```http
GET /api/v1.0/sessionentry/enterSession?code={inviteToken}
```

**Response:**
```json
{
  "sessionName": "My Chat Session",
  "sessionDescription": "Join our chat session",
  "currentParticipants": 15,
  "maxParticipants": 36,
  "estimatedWaitTime": 0,
  "canJoin": true,
  "requiresEmailAndName": true
}
```

#### Join Session Anonymously
```http
POST /api/v1.0/sessionentry/joinSession
Content-Type: application/json

{
  "inviteCode": "32_char_invite_token",
  "email": "user@example.com",
  "displayName": "Anonymous User"
}
```

**Response:**
```json
{
  "success": true,
  "participantId": "participant_uuid",
  "roomId": "room_uuid",
  "sessionId": "session_uuid",
  "signalRConnectionToken": "signalr_token",
  "message": "Successfully joined session"
}
```

#### Check Room Availability
```http
GET /api/v1.0/sessionentry/session/{sessionId}/rooms/availability
```

---

### 5. Room Management

#### List Session Rooms
```http
GET /api/v1.0/chat/rooms/session/{sessionId}
Authorization: Bearer <jwt_token>
```

#### Get Room Details
```http
GET /api/v1.0/chat/rooms/{roomId}
Authorization: Bearer <jwt_token>
```

#### List Room Participants
```http
GET /api/v1.0/chat/rooms/{roomId}/participants
Authorization: Bearer <jwt_token>
```

#### Get Room Messages
```http
GET /api/v1.0/chat/rooms/{roomId}/messages
Authorization: Bearer <jwt_token>

Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 50, max: 100)
- since: datetime (ISO 8601)
```

#### Get Recent Room Messages
```http
GET /api/v1.0/chat/rooms/{roomId}/messages/recent
Authorization: Bearer <jwt_token>

Query Parameters:
- count: int (default: 20, max: 100)
```

#### Get Room Statistics
```http
GET /api/v1.0/chat/rooms/{roomId}/statistics
Authorization: Bearer <jwt_token>
```

#### Check Room Availability
```http
GET /api/v1.0/chat/rooms/{roomId}/availability
Authorization: Bearer <jwt_token>
```

#### List Available Rooms in Session
```http
GET /api/v1.0/chat/rooms/session/{sessionId}/available
Authorization: Bearer <jwt_token>
```

---

### 6. Real-time Messaging (SignalR)

#### Connection Endpoint
```
WebSocket: /chathub
HTTP Fallback: /chathub/negotiate
```

#### Connection Methods

**For Authenticated Users:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
        accessTokenFactory: () => jwtToken
    })
    .build();
```

**For Anonymous Users:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();
```

#### Hub Methods (Client → Server)

##### Join Room with Token
```javascript
await connection.invoke("JoinRoomWithToken", inviteToken, displayName);
```

**Response:**
```json
{
  "success": true,
  "roomId": "room_uuid",
  "participantId": "participant_uuid",
  "message": "Successfully joined room"
}
```

##### Send Message
```javascript
await connection.invoke("SendMessage", messageContent, roomId);
```

**Message Content Structure:**
```json
{
  "content": "Hello everyone! GUID: 123e4567-e89b-12d3-a456-426614174000",
  "messageType": "text",
  "guid": "123e4567-e89b-12d3-a456-426614174000"
}
```

##### Leave Room
```javascript
await connection.invoke("LeaveRoom", roomId);
```

##### Get Room Participants
```javascript
const participants = await connection.invoke("GetRoomParticipants", roomId);
```

#### Hub Events (Server → Client)

##### Message Received
```javascript
connection.on("MessageReceived", (message) => {
  // message structure:
  {
    "id": "message_uuid",
    "content": "Hello everyone!",
    "senderId": "participant_uuid",
    "senderName": "John Doe",
    "roomId": "room_uuid",
    "timestamp": "2025-09-23T20:30:00Z",
    "messageType": "text",
    "guid": "123e4567-e89b-12d3-a456-426614174000",
    "isAnonymous": false
  }
});
```

##### Participant Joined
```javascript
connection.on("ParticipantJoined", (participant) => {
  // participant structure:
  {
    "id": "participant_uuid",
    "displayName": "New User",
    "joinedAt": "2025-09-23T20:30:00Z",
    "isAnonymous": true,
    "roomId": "room_uuid"
  }
});
```

##### Participant Left
```javascript
connection.on("ParticipantLeft", (participant) => {
  // Same structure as ParticipantJoined
});
```

##### Room Status Updated
```javascript
connection.on("RoomStatusUpdated", (roomStatus) => {
  {
    "roomId": "room_uuid",
    "participantCount": 5,
    "maxParticipants": 6,
    "canJoin": true,
    "isActive": true
  }
});
```

##### Connection Error
```javascript
connection.on("ConnectionError", (error) => {
  {
    "code": "ROOM_FULL",
    "message": "Room has reached maximum capacity",
    "timestamp": "2025-09-23T20:30:00Z"
  }
});
```

---

## Complete User Flow Examples

### 1. Anonymous User Join Flow

```javascript
// Step 1: User accesses invite URL
const inviteCode = "abc123def456..."; // 32+ character token
const response = await fetch(`/api/v1.0/sessionentry/enterSession?code=${inviteCode}`);
const sessionInfo = await response.json();

// Step 2: User provides email and display name
const joinResponse = await fetch('/api/v1.0/sessionentry/joinSession', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    inviteCode: inviteCode,
    email: 'user@example.com',
    displayName: 'Anonymous User'
  })
});
const joinResult = await joinResponse.json();

// Step 3: Connect to SignalR
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/chathub')
  .build();

await connection.start();

// Step 4: Join room with invite token
const roomJoinResult = await connection.invoke("JoinRoomWithToken", inviteCode, "Anonymous User");

// Step 5: Set up message handlers
connection.on("MessageReceived", handleMessage);
connection.on("ParticipantJoined", handleParticipantJoined);
connection.on("ParticipantLeft", handleParticipantLeft);

// Step 6: Send messages with GUID tracking
const messageGuid = generateUUID();
await connection.invoke("SendMessage", `Hello! GUID: ${messageGuid}`, roomJoinResult.roomId);
```

### 2. Admin Session Creation Flow

```javascript
// Step 1: Authenticate
const authResponse = await fetch('/api/v1.0/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'admin@wagl.ai',
    password: 'SecurePass123!'
  })
});
const auth = await authResponse.json();

// Step 2: Create session
const sessionResponse = await fetch('/api/v1.0/chat/sessions', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${auth.accessToken}`
  },
  body: JSON.stringify({
    name: 'My Chat Session',
    scheduledStartTime: new Date(Date.now() + 3600000).toISOString(), // 1 hour from now
    durationMinutes: 60,
    maxParticipants: 18,
    maxParticipantsPerRoom: 6
  })
});
const session = await sessionResponse.json();

// Step 3: Generate invite tokens
const inviteResponse = await fetch('/api/v1.0/chat/invites', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${auth.accessToken}`
  },
  body: JSON.stringify({
    sessionId: session.id,
    expirationMinutes: 120
  })
});
const invite = await inviteResponse.json();

// Step 4: Start session
await fetch(`/api/v1.0/chat/sessions/${session.id}/start`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${auth.accessToken}` }
});

// Step 5: Share invite URL
const inviteUrl = `https://app.wagl.ai/join?code=${invite.token}`;
```

### 3. Message Tracking and Room Isolation

```javascript
// Generate unique GUID for message tracking
function generateMessageGUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c == 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

// Send tracked message
const messageGuid = generateMessageGUID();
const messageContent = `User message with tracking ID: ${messageGuid}`;

await connection.invoke("SendMessage", messageContent, currentRoomId);

// Track sent messages for verification
const sentMessages = [];
sentMessages.push({
  guid: messageGuid,
  content: messageContent,
  roomId: currentRoomId,
  timestamp: new Date().toISOString(),
  sender: displayName
});

// Verify message isolation
connection.on("MessageReceived", (message) => {
  // Verify message belongs to current room
  if (message.roomId !== currentRoomId) {
    console.error('Message leak detected! Message from wrong room:', message);
  }

  // Track received messages for GUID verification
  const matchingSent = sentMessages.find(sent =>
    message.content.includes(sent.guid)
  );

  if (matchingSent) {
    console.log('GUID tracking verified:', matchingSent.guid);
  }
});
```

---

## Error Handling

### Common Error Responses

#### 400 Bad Request
```json
{
  "error": "VALIDATION_ERROR",
  "message": "Invalid request parameters",
  "details": {
    "field": "email",
    "message": "Invalid email format"
  }
}
```

#### 401 Unauthorized
```json
{
  "error": "UNAUTHORIZED",
  "message": "Invalid or expired authentication token"
}
```

#### 403 Forbidden
```json
{
  "error": "INSUFFICIENT_PERMISSIONS",
  "message": "User does not have required permissions"
}
```

#### 404 Not Found
```json
{
  "error": "RESOURCE_NOT_FOUND",
  "message": "Session not found"
}
```

#### 409 Conflict
```json
{
  "error": "ROOM_FULL",
  "message": "Room has reached maximum capacity",
  "currentParticipants": 6,
  "maxParticipants": 6
}
```

#### 500 Internal Server Error
```json
{
  "error": "INTERNAL_ERROR",
  "message": "An unexpected error occurred"
}
```

### Anonymous Flow Error Codes

- `INVALID_CODE_FORMAT`: Invite code format is invalid
- `INVITE_NOT_FOUND`: Invite token not found or expired
- `SESSION_NOT_ACTIVE`: Session is not currently active
- `ROOM_FULL`: All rooms in session are at capacity
- `JOIN_FAILED`: Failed to join session for unknown reason
- `DUPLICATE_EMAIL`: Email already used in this session

---

## Rate Limiting

### User Tiers
- **Tier1**: 100 requests/hour
- **Tier2**: 500 requests/hour
- **Tier3**: 2000 requests/hour
- **Anonymous**: 50 requests/hour per IP

### Rate Limit Headers
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1632447600
```

---

## WebSocket Connection Management

### Connection States
- **Connecting**: Initial connection attempt
- **Connected**: Successfully connected to hub
- **Disconnected**: Connection lost or closed
- **Reconnecting**: Automatic reconnection in progress

### Automatic Reconnection
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/chathub')
  .withAutomaticReconnect()
  .build();

connection.onreconnecting(error => {
  console.log('Reconnecting...', error);
});

connection.onreconnected(connectionId => {
  console.log('Reconnected with ID:', connectionId);
  // Rejoin rooms if necessary
});
```

### Connection Monitoring
```javascript
connection.on("ConnectionError", (error) => {
  console.error('SignalR connection error:', error);
  // Handle connection errors
});

connection.onclose(error => {
  console.log('Connection closed:', error);
  // Handle disconnection
});
```

---

## Data Models

### User Model
```typescript
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  tierLevel: number;
  availableFeatures: string[];
  hourlyRateLimit: number;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  subscriptionExpiresAt: string | null;
}
```

### Session Model
```typescript
interface ChatSession {
  id: string;
  name: string;
  status: 'Scheduled' | 'Active' | 'Ended' | 'Cancelled';
  scheduledStartTime: string;
  startedAt: string | null;
  endedAt: string | null;
  durationMinutes: number;
  maxParticipants: number;
  maxParticipantsPerRoom: number;
  currentParticipants: number;
  createdAt: string;
  createdBy: string;
  canStart: boolean;
  isActive: boolean;
  isExpired: boolean;
}
```

### Room Model
```typescript
interface ChatRoom {
  id: string;
  name: string;
  sessionId: string;
  currentParticipants: number;
  maxParticipants: number;
  canJoin: boolean;
  isActive: boolean;
  createdAt: string;
}
```

### Message Model
```typescript
interface ChatMessage {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  roomId: string;
  timestamp: string;
  messageType: 'text' | 'system' | 'notification';
  guid: string;
  isAnonymous: boolean;
  isDeleted: boolean;
}
```

### Participant Model
```typescript
interface Participant {
  id: string;
  displayName: string;
  email: string | null;
  userId: string | null;
  sessionId: string;
  roomId: string;
  joinedAt: string;
  leftAt: string | null;
  isAnonymous: boolean;
  isActive: boolean;
  connectionId: string | null;
}
```

---

## Health Check

```http
GET /health
```

**Response:**
```
Healthy
```

---

## Environment Information

- **Production URL**: `http://wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com`
- **API Version**: v1.0
- **Database**: PostgreSQL (Aurora Serverless)
- **Cache**: AWS ElastiCache Serverless (ValKey/Redis) - **TLS Required**
- **Real-time**: SignalR WebSockets
- **Authentication**: JWT + API Key (dual scheme)

---

## Notes

1. **All timestamps are in ISO 8601 format (UTC)**
2. **Invite tokens must be at least 32 characters long**
3. **Anonymous users are automatically assigned to rooms based on availability**
4. **Message GUIDs are used for tracking and preventing duplication**
5. **SignalR connections support automatic reconnection**
6. **Rate limiting is enforced per user tier**
7. **Sessions support up to 6 rooms with 6 participants each (36 total)**
8. **TLS is required for cache connections in production**