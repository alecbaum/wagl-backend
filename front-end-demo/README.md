# Wagl Backend API Demo - Frontend

A comprehensive web-based UI for testing the Wagl Backend API in production. This demo provides interfaces for authentication, session management, real-time chat, and all API endpoints.

## Features

- **Authentication**: Login, registration, JWT token management
- **Anonymous Access**: Join sessions with invite codes
- **Dashboard**: Session/room management with real-time statistics
- **Real-Time Chat**: WebSocket messaging via SignalR
- **Invite System**: Generate and validate invite codes
- **Multi-Role Support**: Admin, user, moderator, bot, and anonymous access
- **Responsive Design**: Works on desktop and mobile devices

## Quick Start with Docker

### Prerequisites
- Docker and Docker Compose installed
- Port 3000 available on your machine

### Run the Demo

1. **Build and start the container:**
   ```bash
   cd front-end-demo
   docker-compose up --build
   ```

2. **Access the demo:**
   - Open your browser to: http://localhost:3000
   - The demo will connect to the production API at `https://api.wagl.ai`

3. **Stop the demo:**
   ```bash
   docker-compose down
   ```

### Development Mode

For development with live file watching:

```bash
# Start with volume mounting for live updates
docker-compose up --build

# Make changes to files - they'll be reflected immediately
# No need to rebuild unless you change Dockerfile or nginx.conf
```

## Configuration

### API Endpoints
The demo can test against two endpoints (configurable in the UI):
- **Primary**: `https://api.wagl.ai` (custom domain)
- **Direct**: `https://v6uwnty3vi.us-east-1.awsapprunner.com` (App Runner URL)

### CORS Proxy (Optional)
If you encounter CORS issues, enable the CORS proxy:

```bash
docker-compose --profile cors-proxy up --build
```

This starts a CORS proxy on port 8080. Update the API configuration in `js/config.js` to use `http://localhost:8080/` as a proxy prefix.

## Testing Scenarios

### 1. Admin User Testing
- Register a new admin account
- Create chat sessions
- Generate invite codes
- Monitor dashboard statistics
- Manage rooms and participants

### 2. Regular User Testing
- Login with user credentials
- Join existing sessions
- Participate in chat rooms
- Test real-time messaging

### 3. Anonymous Testing
- Use invite codes to join without registration
- Test anonymous chat participation
- Validate invite code functionality

### 4. Real-Time Features
- Test WebSocket connections
- Verify message delivery
- Check typing indicators
- Monitor connection status

## File Structure

```
front-end-demo/
├── index.html          # Main application UI
├── css/
│   ├── styles.css      # Core styling
│   └── components.css  # UI components
├── js/
│   ├── config.js       # API configuration
│   ├── utils.js        # Utility functions
│   ├── api-client.js   # HTTP API client
│   ├── auth-manager.js # Authentication logic
│   ├── signalr-manager.js # Real-time messaging
│   ├── ui-components.js # UI management
│   └── app.js          # Main application
├── Dockerfile          # Container configuration
├── docker-compose.yml  # Docker Compose setup
├── nginx.conf          # Web server configuration
└── README.md          # This file
```

## API Coverage

The demo tests all major API endpoints:

**Authentication:**
- POST /auth/login
- POST /auth/register
- POST /auth/refresh
- POST /auth/logout

**Sessions:**
- GET /sessions
- POST /sessions
- PUT /sessions/{id}
- DELETE /sessions/{id}
- POST /sessions/{id}/start
- POST /sessions/{id}/end

**Rooms:**
- GET /rooms
- POST /rooms
- PUT /rooms/{id}
- DELETE /rooms/{id}
- POST /rooms/{id}/join
- POST /rooms/{id}/leave
- GET /rooms/{id}/participants
- GET /rooms/{id}/messages

**Invites:**
- GET /invites
- POST /invites
- POST /invites/validate
- POST /invites/join
- DELETE /invites/{id}

**Real-Time (SignalR):**
- JoinRoom
- LeaveRoom
- SendMessage
- ReceiveMessage
- UserJoined/Left events

## Troubleshooting

### Connection Issues
- Ensure the backend API is running
- Check browser console for CORS errors
- Verify SignalR WebSocket connections
- Try switching between primary/direct API endpoints

### Authentication Problems
- Clear browser local storage: `localStorage.clear()`
- Check JWT token expiration
- Verify API endpoint connectivity

### Real-Time Chat Issues
- Check SignalR connection status indicator
- Verify WebSocket support in browser
- Look for TLS/SSL certificate issues

### Docker Issues
- Ensure Docker is running
- Check port 3000 is not in use: `lsof -i :3000`
- Rebuild container: `docker-compose build --no-cache`

## Production Notes

This demo is configured for testing against the production Wagl Backend API. It includes:

- Proper JWT token handling with automatic refresh
- Rate limiting awareness and error handling
- Secure HTTPS connections to production endpoints
- Production-ready nginx configuration with security headers

## Support

For issues with the frontend demo, check the browser console for errors and verify the backend API is accessible at the configured endpoints.