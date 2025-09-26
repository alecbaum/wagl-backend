# Wagl Frontend Demo Testing Guide

## 🚀 Frontend Testing Status

### Backend Status: ✅ READY
- **Health**: API is healthy and responding
- **Authentication**: Working properly with JWT tokens
- **Endpoint**: https://v6uwnty3vi.us-east-1.awsapprunner.com
- **Frontend**: Running on http://localhost:3000

### Test Credentials
✅ **Working User Account:**
- **Email**: `test@example.com`
- **Password**: `TestPass123`
- **Tier**: Tier1 (100 requests/hour)
- **Role**: Tier1 User (limited permissions)

⚠️ **Seeded Admin Accounts**: Not working (seeding may have failed)
- `admin@wagl.com` - Login fails
- `moderator@wagl.com` - Login fails
- `tier2@wagl.com` - Login fails

## 🧪 Manual Frontend Testing Steps

### 1. Access the Frontend
```bash
# Frontend should already be running on:
http://localhost:3000
```

### 2. Test Basic Authentication Flow

#### Test User Login
1. Go to http://localhost:3000
2. Click on the **Login** tab
3. Enter credentials:
   - Email: `test@example.com`
   - Password: `TestPass123`
4. Click **Login**

**Expected Results:**
- ✅ Successful login
- ✅ Redirect to dashboard
- ✅ User name displayed in top nav
- ✅ JWT token stored in browser

#### Test User Registration
1. Click on the **Register** tab
2. Fill out the form:
   - First Name: `Demo`
   - Last Name: `User`
   - Email: `demo@example.com`
   - Password: `DemoPass123`
   - Confirm Password: `DemoPass123`
   - Tier: `Tier 1`
3. Click **Register**

**Expected Results:**
- ✅ Account created
- ✅ Automatic login after registration
- ✅ Redirect to dashboard

### 3. Test Dashboard Features

#### Dashboard Stats
1. After logging in, check the dashboard stats
2. Look for:
   - Total Sessions count
   - Active Sessions count
   - Total Participants count

**Expected Results:**
- ⚠️ May show 0s if no data exists
- ✅ No authentication errors

#### Dashboard Tabs
Test each tab in the dashboard:

1. **Sessions Tab**
   - Should show list of user's sessions
   - "Create Session" button should be visible
   - May show empty list for new users

2. **Rooms Tab**
   - Should show available rooms
   - Session filter dropdown should be present

3. **Invites Tab**
   - Should show user's invites
   - "Generate Invite" button should be visible

4. **Moderators Tab**
   - Should show provider list
   - "Create Moderator" button may be restricted

5. **Live Chat Tab**
   - Should show chat interface
   - May need active rooms to test fully

### 4. Test Session Creation (Limited Permissions)

1. Click **Create Session** button
2. Fill out the form:
   - Session Name: `Test Session`
   - Scheduled Start Time: (future time)
   - Duration: `60` minutes
   - Max Participants: `18`
   - Max Per Room: `6`
3. Click **Create Session**

**Expected Results:**
- ⚠️ May fail with permission error (non-admin user)
- ✅ Shows appropriate error message
- ❌ OR succeeds if permissions allow

### 5. Test Anonymous Join Flow

1. Click **Anonymous Join** tab
2. Enter a fake invite code: `1234567890123456789012345678901234567890`
3. Click **Validate**

**Expected Results:**
- ❌ Should show "Invalid invite code" error
- ✅ Frontend handles error gracefully

### 6. Test Moderator API Key Flow

1. Click **Moderator (API Key)** tab
2. Enter a fake API key: `fake-api-key-12345`
3. Click **Authenticate**

**Expected Results:**
- ❌ Should show authentication error
- ✅ Frontend handles error gracefully

### 7. Test Public Dashboard

1. Click **Public Dashboard** button in top nav
2. Check that it loads without requiring authentication
3. Test the API Test tab

**Expected Results:**
- ✅ Loads without authentication
- ✅ Shows system stats
- ✅ API test shows various endpoint results

## 🔧 Admin Functionality Testing

### Creating an Admin User (Workaround)

Since seeded admin accounts aren't working, you can:

1. **Register a new user** as shown above
2. **Contact system admin** to manually elevate permissions
3. **Or use database tools** to add admin role to existing user

### Expected Admin Features (Once Admin Access is Available)

1. **Session Creation**: Should work without permission errors
2. **Provider Management**: Should be able to create/manage moderators
3. **Full Dashboard Access**: All stats and controls should be available
4. **Invite Generation**: Should work for any session

## 🌐 Real-time Messaging Testing

### Prerequisites
- Need at least one active session
- Need invite codes for anonymous users
- May need multiple browser windows/tabs

### Steps
1. Create or join a session
2. Open multiple browser tabs
3. Test sending messages
4. Verify real-time delivery

**Expected Results:**
- ✅ Messages appear in real-time
- ✅ Participant list updates
- ✅ SignalR connection shows as connected

## 🚨 Known Issues and Limitations

### Authentication Issues
- ⚠️ **Seeded users don't work**: Use newly registered users instead
- ⚠️ **Limited permissions**: Test user has Tier1 access only
- ⚠️ **Admin features limited**: Need proper admin role for full testing

### API Endpoints
- ⚠️ **Some 404 errors**: Some endpoints may not be implemented
- ⚠️ **Some 401 errors**: Permission issues or authentication scheme mismatches
- ✅ **Core auth works**: Login/registration/JWT generation working

### Frontend Features
- ✅ **UI is complete**: All pages and modals are implemented
- ✅ **API integration**: Frontend correctly calls backend endpoints
- ⚠️ **Real-time features**: Need proper session setup to test SignalR

## 🎯 Test Priorities

### High Priority (Core Functionality)
1. ✅ User login/registration
2. ✅ Dashboard access
3. ⚠️ Session creation (admin required)
4. ⚠️ Real-time messaging

### Medium Priority (Enhanced Features)
1. ⚠️ Invite generation and validation
2. ⚠️ Anonymous user flow
3. ⚠️ Provider/moderator management

### Low Priority (Nice to Have)
1. ⚠️ Advanced admin features
2. ⚠️ Analytics and reporting
3. ⚠️ API key authentication

## 🔗 Quick Access Links

- **Frontend Demo**: http://localhost:3000
- **Backend API**: https://v6uwnty3vi.us-east-1.awsapprunner.com
- **API Health**: https://v6uwnty3vi.us-east-1.awsapprunner.com/health
- **Test Login**: Email: `test@example.com`, Password: `TestPass123`

## 📝 Testing Checklist

- [ ] Frontend loads successfully
- [ ] User can register new account
- [ ] User can login with credentials
- [ ] Dashboard shows without errors
- [ ] Session creation (test permissions)
- [ ] Chat interface loads
- [ ] Real-time messaging works
- [ ] Anonymous join flow works
- [ ] Admin features work (if admin access available)
- [ ] Error handling is graceful
- [ ] SignalR connection established

---

**Ready for testing!** The backend is healthy and authentication is working. Use the test credentials above to explore the frontend functionality.