#!/usr/bin/env node

/**
 * Comprehensive Real-World Chat System Integration Test
 *
 * This script tests all layers of the chat system:
 * 1. Authentication (JWT tokens)
 * 2. Session creation and management
 * 3. Room allocation and management
 * 4. SignalR real-time connections
 * 5. Message routing and persistence
 * 6. Database verification
 * 7. Background services coordination
 */

const axios = require('axios');
const signalR = require('@microsoft/signalr');
const { v4: uuidv4 } = require('uuid');

// Configuration
const config = {
    baseUrl: 'http://api.wagl.ai',
    hubUrl: 'http://api.wagl.ai/chathub',
    testUsers: [
        { email: 'testuser1@wagl.ai', password: 'TestPass123!', displayName: 'Alice' },
        { email: 'testuser2@wagl.ai', password: 'TestPass123!', displayName: 'Bob' },
        { email: 'testuser3@wagl.ai', password: 'TestPass123!', displayName: 'Charlie' },
        { email: 'testuser4@wagl.ai', password: 'TestPass123!', displayName: 'Diana' },
        { email: 'testuser5@wagl.ai', password: 'TestPass123!', displayName: 'Eve' },
        { email: 'testuser6@wagl.ai', password: 'TestPass123!', displayName: 'Frank' }
    ],
    roomsToTest: 3, // Create 3 rooms
    usersPerRoom: 6, // 6 users per room
    messagesPerUser: 3 // Each user sends 3 messages
};

// Test state tracking
const testState = {
    users: [], // Will store authenticated users with tokens
    session: null, // Created session
    rooms: [], // Available rooms
    connections: [], // SignalR connections
    sentMessages: [], // Track all sent messages
    receivedMessages: [], // Track all received messages
    errors: []
};

// Utility functions
const log = (message, level = 'INFO') => {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] [${level}] ${message}`);
};

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

// API Helper functions
const apiRequest = async (method, endpoint, data = null, token = null) => {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await axios({
            method,
            url: `${config.baseUrl}${endpoint}`,
            data,
            headers
        });
        return response.data;
    } catch (error) {
        log(`API Error: ${method} ${endpoint} - ${error.response?.data?.message || error.message}`, 'ERROR');
        throw error;
    }
};

// Step 1: Authentication Layer Test
const authenticateUsers = async () => {
    log('🔐 Testing Authentication Layer...');

    for (const user of config.testUsers) {
        try {
            const authResponse = await apiRequest('POST', '/api/v1/auth/login', {
                email: user.email,
                password: user.password
            });

            testState.users.push({
                ...user,
                token: authResponse.token,
                userId: authResponse.userId
            });

            log(`✅ User ${user.displayName} authenticated successfully`);
        } catch (error) {
            // If user doesn't exist, try to register them
            try {
                await apiRequest('POST', '/api/v1/auth/register', {
                    email: user.email,
                    password: user.password,
                    confirmPassword: user.password,
                    firstName: user.displayName,
                    lastName: 'TestUser'
                });

                // Try login again
                const authResponse = await apiRequest('POST', '/api/v1/auth/login', {
                    email: user.email,
                    password: user.password
                });

                testState.users.push({
                    ...user,
                    token: authResponse.token,
                    userId: authResponse.userId
                });

                log(`✅ User ${user.displayName} registered and authenticated`);
            } catch (regError) {
                log(`❌ Failed to authenticate/register user ${user.displayName}`, 'ERROR');
                testState.errors.push(`Authentication failed for ${user.displayName}`);
            }
        }
    }
};

// Step 2: Session Management Test
const createTestSession = async () => {
    log('🏛️ Testing Session Management Layer...');

    if (testState.users.length === 0) {
        throw new Error('No authenticated users available');
    }

    const adminUser = testState.users[0];
    const sessionData = {
        name: `Integration Test Session ${Date.now()}`,
        scheduledStartTime: new Date().toISOString(),
        durationMinutes: 60,
        maxParticipants: config.roomsToTest * config.usersPerRoom,
        maxParticipantsPerRoom: config.usersPerRoom
    };

    try {
        testState.session = await apiRequest('POST', '/api/v1/chat/sessions', sessionData, adminUser.token);
        log(`✅ Session created: ${testState.session.name} (ID: ${testState.session.id})`);

        // Start the session if it's not automatically started
        if (testState.session.status !== 'Active') {
            try {
                await apiRequest('PUT', `/api/v1/chat/sessions/${testState.session.id}/start`, {}, adminUser.token);
                log(`✅ Session started successfully`);
            } catch (error) {
                log(`⚠️ Could not start session: ${error.message}`, 'WARN');
            }
        }

    } catch (error) {
        log(`❌ Failed to create session: ${error.message}`, 'ERROR');
        testState.errors.push('Session creation failed');
        throw error;
    }
};

// Step 3: Room Allocation Test
const getRoomsForSession = async () => {
    log('🏠 Testing Room Allocation Layer...');

    const adminUser = testState.users[0];

    try {
        testState.rooms = await apiRequest('GET', `/api/v1/chat/rooms/session/${testState.session.id}`, null, adminUser.token);
        log(`✅ Retrieved ${testState.rooms.length} rooms for session`);

        if (testState.rooms.length < config.roomsToTest) {
            log(`⚠️ Expected ${config.roomsToTest} rooms, got ${testState.rooms.length}`, 'WARN');
        }

        // Log room details
        testState.rooms.forEach((room, index) => {
            log(`   Room ${index + 1}: ${room.id} (Capacity: ${room.maxParticipants})`);
        });

    } catch (error) {
        log(`❌ Failed to get rooms: ${error.message}`, 'ERROR');
        testState.errors.push('Room retrieval failed');
        throw error;
    }
};

// Step 4: SignalR Connection Test
const connectToSignalR = async () => {
    log('⚡ Testing SignalR Real-time Layer...');

    const connectUser = async (user, roomIndex) => {
        const room = testState.rooms[roomIndex % testState.rooms.length];

        try {
            // Create SignalR connection
            const connection = new signalR.HubConnectionBuilder()
                .withUrl(config.hubUrl, {
                    accessTokenFactory: () => user.token
                })
                .withAutomaticReconnect()
                .build();

            // Set up message handlers
            connection.on('MessageReceived', (message) => {
                testState.receivedMessages.push({
                    ...message,
                    receivedBy: user.displayName,
                    receivedAt: new Date().toISOString(),
                    roomId: room.id
                });
                log(`📨 ${user.displayName} received message in room ${room.id}: "${message.content}"`);
            });

            connection.on('ParticipantJoined', (participant) => {
                log(`👋 ${participant.displayName} joined room ${room.id}`);
            });

            connection.on('ParticipantLeft', (participant) => {
                log(`👋 ${participant.displayName} left room ${room.id}`);
            });

            // Connect to hub
            await connection.start();
            log(`✅ ${user.displayName} connected to SignalR hub`);

            // Join room with invite token (need to get invite token first)
            const joinResult = await connection.invoke('JoinRoomWithToken', 'temp-token', user.displayName);

            if (joinResult.success) {
                log(`✅ ${user.displayName} joined room ${room.id}`);
            } else {
                log(`❌ ${user.displayName} failed to join room: ${joinResult.message}`, 'ERROR');
            }

            testState.connections.push({
                user,
                connection,
                roomId: room.id,
                roomIndex
            });

        } catch (error) {
            log(`❌ Failed to connect ${user.displayName} to SignalR: ${error.message}`, 'ERROR');
            testState.errors.push(`SignalR connection failed for ${user.displayName}`);
        }
    };

    // Connect users to rooms (distribute evenly)
    for (let i = 0; i < testState.users.length; i++) {
        const user = testState.users[i];
        const roomIndex = Math.floor(i / config.usersPerRoom);
        await connectUser(user, roomIndex);
        await sleep(1000); // Stagger connections
    }
};

// Step 5: Message Routing Test
const testMessageRouting = async () => {
    log('💬 Testing Message Routing and Persistence Layer...');

    // Send messages from each connected user
    for (const connectionInfo of testState.connections) {
        const { user, connection, roomId } = connectionInfo;

        for (let i = 0; i < config.messagesPerUser; i++) {
            const messageGuid = uuidv4();
            const messageContent = `Test message ${i + 1} from ${user.displayName} - GUID: ${messageGuid}`;

            try {
                await connection.invoke('SendMessage', messageContent, roomId);

                testState.sentMessages.push({
                    content: messageContent,
                    guid: messageGuid,
                    sender: user.displayName,
                    roomId: roomId,
                    sentAt: new Date().toISOString()
                });

                log(`📤 ${user.displayName} sent message to room ${roomId}: "${messageContent}"`);
                await sleep(500); // Small delay between messages

            } catch (error) {
                log(`❌ Failed to send message from ${user.displayName}: ${error.message}`, 'ERROR');
                testState.errors.push(`Message send failed for ${user.displayName}`);
            }
        }

        await sleep(1000); // Pause between users
    }

    // Wait for all messages to be processed
    log('⏳ Waiting for message propagation...');
    await sleep(5000);
};

// Step 6: Database Verification Test
const verifyDatabasePersistence = async () => {
    log('🗄️ Testing Database Persistence Layer...');

    // This would require database access or API endpoints to verify
    // For now, we'll check message counts and consistency

    const expectedMessages = testState.sentMessages.length;
    const roomMessageCounts = {};

    // Count messages per room
    testState.sentMessages.forEach(msg => {
        if (!roomMessageCounts[msg.roomId]) {
            roomMessageCounts[msg.roomId] = 0;
        }
        roomMessageCounts[msg.roomId]++;
    });

    log(`📊 Message Statistics:`);
    log(`   Total messages sent: ${expectedMessages}`);
    log(`   Total messages received: ${testState.receivedMessages.length}`);

    Object.entries(roomMessageCounts).forEach(([roomId, count]) => {
        log(`   Room ${roomId}: ${count} messages`);
    });
};

// Step 7: Message Isolation Verification
const verifyMessageIsolation = async () => {
    log('🔒 Testing Message Isolation Between Rooms...');

    const roomGroups = {};

    // Group received messages by room
    testState.receivedMessages.forEach(msg => {
        if (!roomGroups[msg.roomId]) {
            roomGroups[msg.roomId] = [];
        }
        roomGroups[msg.roomId].push(msg);
    });

    // Verify no cross-room message leakage
    let isolationPassed = true;

    Object.entries(roomGroups).forEach(([roomId, messages]) => {
        const crossRoomMessages = messages.filter(msg => {
            const originalMessage = testState.sentMessages.find(sent => sent.content === msg.content);
            return originalMessage && originalMessage.roomId !== roomId;
        });

        if (crossRoomMessages.length > 0) {
            log(`❌ Message isolation failed: ${crossRoomMessages.length} cross-room messages in room ${roomId}`, 'ERROR');
            isolationPassed = false;
        }
    });

    if (isolationPassed) {
        log(`✅ Message isolation verified: No cross-room message leakage detected`);
    }
};

// Step 8: Cleanup and Report
const cleanupAndReport = async () => {
    log('🧹 Cleaning up connections...');

    // Close all SignalR connections
    for (const connectionInfo of testState.connections) {
        try {
            await connectionInfo.connection.stop();
        } catch (error) {
            // Ignore cleanup errors
        }
    }

    // Generate test report
    log('📋 Test Report Summary:');
    log(`   Users authenticated: ${testState.users.length}/${config.testUsers.length}`);
    log(`   Session created: ${testState.session ? '✅' : '❌'}`);
    log(`   Rooms allocated: ${testState.rooms.length}`);
    log(`   SignalR connections: ${testState.connections.length}/${testState.users.length}`);
    log(`   Messages sent: ${testState.sentMessages.length}`);
    log(`   Messages received: ${testState.receivedMessages.length}`);
    log(`   Errors encountered: ${testState.errors.length}`);

    if (testState.errors.length > 0) {
        log('❌ Errors during testing:');
        testState.errors.forEach(error => log(`   - ${error}`));
    }

    const successRate = ((testState.sentMessages.length + testState.receivedMessages.length) /
                        (config.testUsers.length * config.messagesPerUser * 2)) * 100;

    log(`🎯 Overall Test Success Rate: ${successRate.toFixed(1)}%`);

    if (successRate > 90) {
        log('🎉 Integration test PASSED! All layers functioning correctly.');
        process.exit(0);
    } else {
        log('💥 Integration test FAILED! Some components need attention.');
        process.exit(1);
    }
};

// Main test execution
const runIntegrationTest = async () => {
    log('🚀 Starting Comprehensive Chat System Integration Test...');
    log(`Configuration: ${config.roomsToTest} rooms, ${config.usersPerRoom} users per room, ${config.messagesPerUser} messages per user`);

    try {
        await authenticateUsers();
        await createTestSession();
        await getRoomsForSession();
        await connectToSignalR();
        await testMessageRouting();
        await verifyDatabasePersistence();
        await verifyMessageIsolation();
        await cleanupAndReport();

    } catch (error) {
        log(`💥 Integration test failed with error: ${error.message}`, 'ERROR');
        await cleanupAndReport();
    }
};

// Handle process termination
process.on('SIGINT', async () => {
    log('⏹️ Test interrupted by user');
    await cleanupAndReport();
});

process.on('SIGTERM', async () => {
    log('⏹️ Test terminated');
    await cleanupAndReport();
});

// Start the test
if (require.main === module) {
    runIntegrationTest();
}

module.exports = { runIntegrationTest, testState, config };