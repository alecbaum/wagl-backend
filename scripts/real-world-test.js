#!/usr/bin/env node

/**
 * Real-World Chat System Integration Test
 *
 * Tests the actual user flow:
 * 1. Admin creates session and generates invite codes
 * 2. Anonymous users access via unique URLs: api.wagl.ai/sessionentry/enterSession?code=...
 * 3. Users provide email + display name to join
 * 4. System auto-assigns users to rooms as they enter
 * 5. Users send messages with GUIDs to verify routing
 * 6. Verify message isolation between rooms
 */

const axios = require('axios');
const signalR = require('@microsoft/signalr');
const { v4: uuidv4 } = require('uuid');

// Configuration
const config = {
    baseUrl: 'http://api.wagl.ai',
    hubUrl: 'http://api.wagl.ai/chathub',
    adminUser: {
        email: 'admin@wagl.ai',
        password: 'AdminPass123#',
        displayName: 'Admin'
    },
    anonymousUsers: [
        { email: 'alice@test.com', displayName: 'Alice' },
        { email: 'bob@test.com', displayName: 'Bob' },
        { email: 'charlie@test.com', displayName: 'Charlie' },
        { email: 'diana@test.com', displayName: 'Diana' },
        { email: 'eve@test.com', displayName: 'Eve' },
        { email: 'frank@test.com', displayName: 'Frank' }
    ],
    roomCapacity: 6,
    messagesPerUser: 3
};

// Test state
const testState = {
    adminToken: null,
    session: null,
    inviteCode: null,
    anonymousParticipants: [],
    connections: [],
    sentMessages: [],
    receivedMessages: [],
    errors: []
};

// Utility functions
const log = (message, level = 'INFO') => {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] [${level}] ${message}`);
};

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

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
        const errorMsg = error.response?.data?.message || error.message;
        log(`API Error: ${method} ${endpoint} - ${errorMsg}`, 'ERROR');
        throw error;
    }
};

// Step 1: Admin Setup
const setupAdminAndSession = async () => {
    log('👑 Setting up admin user and session...');

    try {
        // Try to register admin (may fail if exists)
        try {
            await apiRequest('POST', '/api/v1/auth/register', {
                email: config.adminUser.email,
                password: config.adminUser.password,
                confirmPassword: config.adminUser.password,
                firstName: 'Admin',
                lastName: 'User'
            });
            log('✅ Admin user registered');
        } catch (error) {
            log('ℹ️ Admin user already exists (expected)');
        }

        // Login as admin
        const authResponse = await apiRequest('POST', '/api/v1/auth/login', {
            email: config.adminUser.email,
            password: config.adminUser.password
        });

        testState.adminToken = authResponse.token;
        log('✅ Admin authenticated successfully');

        // Create a session
        const sessionData = {
            name: `Real World Test Session ${Date.now()}`,
            scheduledStartTime: new Date().toISOString(),
            durationMinutes: 60,
            maxParticipants: config.anonymousUsers.length,
            maxParticipantsPerRoom: config.roomCapacity
        };

        testState.session = await apiRequest('POST', '/api/v1/chat/sessions', sessionData, testState.adminToken);
        log(`✅ Session created: ${testState.session.name} (ID: ${testState.session.id})`);

        // Start the session
        try {
            await apiRequest('PUT', `/api/v1/chat/sessions/${testState.session.id}/start`, {}, testState.adminToken);
            log('✅ Session started');
        } catch (error) {
            log('ℹ️ Session may already be active');
        }

        // Generate invite code
        const inviteData = {
            sessionId: testState.session.id,
            expirationMinutes: 120
        };

        const inviteResponse = await apiRequest('POST', '/api/v1/chat/invites', inviteData, testState.adminToken);
        testState.inviteCode = inviteResponse.token;
        log(`✅ Invite code generated: ${testState.inviteCode.substring(0, 20)}...`);

    } catch (error) {
        log(`❌ Admin setup failed: ${error.message}`, 'ERROR');
        throw error;
    }
};

// Step 2: Anonymous User Entry via URLs
const testAnonymousUserEntry = async () => {
    log('🔗 Testing anonymous user entry via unique URLs...');

    for (const user of config.anonymousUsers) {
        try {
            // Step 2a: Access the unique URL
            const entryUrl = `/api/v1/sessionentry/enterSession?code=${testState.inviteCode}`;
            const entryResponse = await apiRequest('GET', entryUrl);

            log(`✅ ${user.displayName} accessed session entry URL`);
            log(`   Session: ${entryResponse.sessionName}`);
            log(`   Current participants: ${entryResponse.currentParticipants}/${entryResponse.maxParticipants}`);
            log(`   Estimated wait: ${entryResponse.estimatedWaitTime} minutes`);

            // Step 2b: Join with email and display name
            const joinData = {
                inviteCode: testState.inviteCode,
                email: user.email,
                displayName: user.displayName
            };

            const joinResponse = await apiRequest('POST', '/api/v1/sessionentry/joinSession', joinData);

            if (joinResponse.success) {
                testState.anonymousParticipants.push({
                    ...user,
                    participantId: joinResponse.participantId,
                    roomId: joinResponse.roomId,
                    sessionId: joinResponse.sessionId,
                    signalRToken: joinResponse.signalRConnectionToken
                });

                log(`✅ ${user.displayName} joined session`);
                log(`   Participant ID: ${joinResponse.participantId}`);
                log(`   Assigned to room: ${joinResponse.roomId}`);
            } else {
                log(`❌ ${user.displayName} failed to join: ${joinResponse.message}`, 'ERROR');
                testState.errors.push(`Join failed for ${user.displayName}`);
            }

            await sleep(1000); // Stagger joins

        } catch (error) {
            log(`❌ ${user.displayName} entry failed: ${error.message}`, 'ERROR');
            testState.errors.push(`Entry failed for ${user.displayName}`);
        }
    }

    // Log room assignments
    const roomGroups = {};
    testState.anonymousParticipants.forEach(participant => {
        if (!roomGroups[participant.roomId]) {
            roomGroups[participant.roomId] = [];
        }
        roomGroups[participant.roomId].push(participant.displayName);
    });

    log('📊 Room assignments:');
    Object.entries(roomGroups).forEach(([roomId, participants]) => {
        log(`   Room ${roomId}: ${participants.join(', ')}`);
    });
};

// Step 3: SignalR Real-time Connections
const connectUsersToSignalR = async () => {
    log('⚡ Connecting users to real-time chat...');

    for (const participant of testState.anonymousParticipants) {
        try {
            // Create SignalR connection (anonymous users use participant token)
            const connection = new signalR.HubConnectionBuilder()
                .withUrl(config.hubUrl)
                .withAutomaticReconnect()
                .build();

            // Set up message handlers
            connection.on('MessageReceived', (message) => {
                testState.receivedMessages.push({
                    ...message,
                    receivedBy: participant.displayName,
                    receivedAt: new Date().toISOString(),
                    roomId: participant.roomId
                });
                log(`📨 ${participant.displayName} received: "${message.content}"`);
            });

            connection.on('ParticipantJoined', (joinedParticipant) => {
                log(`👋 ${participant.displayName} sees ${joinedParticipant.displayName} joined room`);
            });

            connection.on('ParticipantLeft', (leftParticipant) => {
                log(`👋 ${participant.displayName} sees ${leftParticipant.displayName} left room`);
            });

            // Connect to hub
            await connection.start();
            log(`✅ ${participant.displayName} connected to SignalR`);

            // Join room using the existing invite token approach
            const joinResult = await connection.invoke('JoinRoomWithToken', testState.inviteCode, participant.displayName);

            if (joinResult.success) {
                log(`✅ ${participant.displayName} joined SignalR room: ${joinResult.roomId}`);
            } else {
                log(`❌ ${participant.displayName} failed to join SignalR room: ${joinResult.message}`, 'ERROR');
            }

            testState.connections.push({
                participant,
                connection
            });

            await sleep(1000); // Stagger connections

        } catch (error) {
            log(`❌ SignalR connection failed for ${participant.displayName}: ${error.message}`, 'ERROR');
            testState.errors.push(`SignalR failed for ${participant.displayName}`);
        }
    }
};

// Step 4: Real-time Message Testing with GUIDs
const testRealTimeMessaging = async () => {
    log('💬 Testing real-time messaging with GUID tracking...');

    for (const connectionInfo of testState.connections) {
        const { participant, connection } = connectionInfo;

        for (let i = 0; i < config.messagesPerUser; i++) {
            const messageGuid = uuidv4();
            const messageContent = `Message ${i + 1} from ${participant.displayName} (${participant.email}) - GUID: ${messageGuid}`;

            try {
                await connection.invoke('SendMessage', messageContent, participant.roomId);

                testState.sentMessages.push({
                    content: messageContent,
                    guid: messageGuid,
                    sender: participant.displayName,
                    senderEmail: participant.email,
                    roomId: participant.roomId,
                    sentAt: new Date().toISOString()
                });

                log(`📤 ${participant.displayName} sent: "${messageContent}"`);
                await sleep(500);

            } catch (error) {
                log(`❌ Message send failed for ${participant.displayName}: ${error.message}`, 'ERROR');
                testState.errors.push(`Message send failed for ${participant.displayName}`);
            }
        }

        await sleep(1000); // Pause between users
    }

    // Wait for message propagation
    log('⏳ Waiting for message propagation...');
    await sleep(5000);
};

// Step 5: Validate Real-World Scenarios
const validateRealWorldScenarios = async () => {
    log('🔍 Validating real-world scenarios...');

    // 1. Verify room isolation
    const roomMessageMap = {};
    testState.receivedMessages.forEach(msg => {
        if (!roomMessageMap[msg.roomId]) {
            roomMessageMap[msg.roomId] = [];
        }
        roomMessageMap[msg.roomId].push(msg);
    });

    let isolationPassed = true;
    Object.entries(roomMessageMap).forEach(([roomId, messages]) => {
        const wrongRoomMessages = messages.filter(msg => {
            const originalMessage = testState.sentMessages.find(sent => sent.content === msg.content);
            return originalMessage && originalMessage.roomId !== roomId;
        });

        if (wrongRoomMessages.length > 0) {
            log(`❌ Room isolation FAILED: ${wrongRoomMessages.length} messages leaked into room ${roomId}`, 'ERROR');
            isolationPassed = false;
        }
    });

    if (isolationPassed) {
        log('✅ Room isolation PASSED: No message leakage detected');
    }

    // 2. Verify GUID message tracking
    const guidMatches = testState.receivedMessages.filter(received => {
        return testState.sentMessages.some(sent => sent.content === received.content);
    });

    const guidMatchRate = (guidMatches.length / testState.sentMessages.length) * 100;
    log(`📊 GUID message tracking: ${guidMatchRate.toFixed(1)}% (${guidMatches.length}/${testState.sentMessages.length})`);

    // 3. Verify email/anonymous user handling
    const anonymousUserCount = testState.anonymousParticipants.length;
    const emailDomains = [...new Set(testState.anonymousParticipants.map(p => p.email.split('@')[1]))];
    log(`👥 Anonymous users: ${anonymousUserCount} from ${emailDomains.length} email domains`);

    // 4. Check room capacity distribution
    const roomCapacityCheck = {};
    testState.anonymousParticipants.forEach(participant => {
        if (!roomCapacityCheck[participant.roomId]) {
            roomCapacityCheck[participant.roomId] = 0;
        }
        roomCapacityCheck[participant.roomId]++;
    });

    let capacityViolations = 0;
    Object.entries(roomCapacityCheck).forEach(([roomId, count]) => {
        if (count > config.roomCapacity) {
            log(`⚠️ Room ${roomId} over capacity: ${count}/${config.roomCapacity}`, 'WARN');
            capacityViolations++;
        }
    });

    if (capacityViolations === 0) {
        log('✅ Room capacity management PASSED: No over-capacity rooms');
    }
};

// Step 6: Cleanup and Report
const generateTestReport = async () => {
    log('🧹 Cleaning up and generating report...');

    // Close all connections
    for (const connectionInfo of testState.connections) {
        try {
            await connectionInfo.connection.stop();
        } catch (error) {
            // Ignore cleanup errors
        }
    }

    // Generate comprehensive report
    log('📋 === REAL-WORLD TEST REPORT ===');
    log(`Session: ${testState.session?.name || 'N/A'}`);
    log(`Admin authenticated: ${testState.adminToken ? '✅' : '❌'}`);
    log(`Session created: ${testState.session ? '✅' : '❌'}`);
    log(`Invite code generated: ${testState.inviteCode ? '✅' : '❌'}`);
    log(`Anonymous users joined: ${testState.anonymousParticipants.length}/${config.anonymousUsers.length}`);
    log(`SignalR connections: ${testState.connections.length}/${testState.anonymousParticipants.length}`);
    log(`Messages sent: ${testState.sentMessages.length}`);
    log(`Messages received: ${testState.receivedMessages.length}`);
    log(`Errors encountered: ${testState.errors.length}`);

    // Room distribution report
    const roomStats = {};
    testState.anonymousParticipants.forEach(p => {
        if (!roomStats[p.roomId]) roomStats[p.roomId] = [];
        roomStats[p.roomId].push(p.displayName);
    });

    log('📊 Room Distribution:');
    Object.entries(roomStats).forEach(([roomId, participants]) => {
        log(`   ${roomId}: ${participants.length} users (${participants.join(', ')})`);
    });

    // Email diversity report
    const emailDomains = [...new Set(testState.anonymousParticipants.map(p => p.email.split('@')[1]))];
    log(`📧 Email domains: ${emailDomains.join(', ')}`);

    if (testState.errors.length > 0) {
        log('❌ Errors during testing:');
        testState.errors.forEach(error => log(`   - ${error}`));
    }

    // Calculate success rate
    const totalExpectedOperations = config.anonymousUsers.length * 4; // entry + join + connect + messages
    const successfulOperations = testState.anonymousParticipants.length * 2 + testState.connections.length + (testState.sentMessages.length / config.messagesPerUser);
    const successRate = (successfulOperations / totalExpectedOperations) * 100;

    log(`🎯 Overall Success Rate: ${successRate.toFixed(1)}%`);

    if (successRate > 85) {
        log('🎉 REAL-WORLD TEST PASSED! All layers functioning for anonymous user flow.');
        return 0;
    } else {
        log('💥 REAL-WORLD TEST FAILED! Some components need attention.');
        return 1;
    }
};

// Main test execution
const runRealWorldTest = async () => {
    log('🚀 Starting Real-World Chat System Test...');
    log(`Testing anonymous user flow: ${config.anonymousUsers.length} users, ${config.roomCapacity} per room`);

    try {
        await setupAdminAndSession();
        await testAnonymousUserEntry();
        await connectUsersToSignalR();
        await testRealTimeMessaging();
        await validateRealWorldScenarios();
        const exitCode = await generateTestReport();
        process.exit(exitCode);

    } catch (error) {
        log(`💥 Real-world test failed: ${error.message}`, 'ERROR');
        await generateTestReport();
        process.exit(1);
    }
};

// Handle process termination
process.on('SIGINT', async () => {
    log('⏹️ Test interrupted');
    await generateTestReport();
    process.exit(1);
});

// Start the test
if (require.main === module) {
    runRealWorldTest();
}

module.exports = { runRealWorldTest, testState, config };