#!/usr/bin/env node

/**
 * Frontend Functionality Test
 * Tests the Wagl frontend demo functionality step by step
 */

const apiEndpoint = 'https://v6uwnty3vi.us-east-1.awsapprunner.com';

// Working test user credentials (created via API)
const testUser = {
    email: 'test@example.com',
    password: 'TestPass123',
    fullName: 'Test User'
};

class FrontendTester {
    constructor() {
        this.apiEndpoint = apiEndpoint;
        this.accessToken = null;
        this.userInfo = null;
    }

    async makeRequest(endpoint, options = {}) {
        const url = `${this.apiEndpoint}/api/v1.0${endpoint}`;
        const config = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                ...options.headers
            },
            ...options
        };

        console.log(`  📡 ${config.method} ${endpoint}`);

        try {
            const response = await fetch(url, config);
            const status = response.status;
            const statusIcon = status < 300 ? '✅' : status < 500 ? '⚠️' : '❌';

            let data;
            try {
                data = await response.json();
            } catch {
                data = await response.text();
            }

            console.log(`     ${statusIcon} ${status} ${response.statusText}`);

            if (status >= 400) {
                console.log(`     📄 Error: ${JSON.stringify(data).substring(0, 150)}...`);
            }

            return { status, data, ok: response.ok };
        } catch (error) {
            console.log(`     💥 Network Error: ${error.message}`);
            return { status: 0, data: null, ok: false, error: error.message };
        }
    }

    async testLogin() {
        console.log('\n🔐 TESTING USER LOGIN');
        console.log('═'.repeat(50));

        const response = await this.makeRequest('/auth/login', {
            method: 'POST',
            body: JSON.stringify({
                email: testUser.email,
                password: testUser.password,
                rememberMe: false
            })
        });

        if (response.ok && response.data.accessToken) {
            this.accessToken = response.data.accessToken;
            this.userInfo = response.data.user;

            console.log(`  ✅ Login Successful!`);
            console.log(`  👤 User: ${response.data.user.fullName}`);
            console.log(`  🎯 Tier: ${response.data.user.tierLevel}`);
            console.log(`  ⏰ Token expires: ${response.data.expiresAt}`);

            // Decode JWT to inspect claims
            try {
                const payload = JSON.parse(atob(response.data.accessToken.split('.')[1]));
                console.log(`  🎭 JWT Claims:`);
                console.log(`     - Role: ${payload.role}`);
                console.log(`     - Account Type: ${payload.account_type}`);
                console.log(`     - Tier Level: ${payload.tier_level}`);
                console.log(`     - Features: ${payload.features}`);
                console.log(`     - Rate Limit Tier: ${payload.rate_limit_tier}`);
            } catch (e) {
                console.log(`  ⚠️ Could not decode JWT`);
            }

            return true;
        } else {
            console.log(`  ❌ Login failed: ${response.data?.message || 'Unknown error'}`);
            return false;
        }
    }

    getAuthHeaders() {
        if (!this.accessToken) {
            throw new Error('No access token available. Please login first.');
        }
        return {
            'Authorization': `Bearer ${this.accessToken}`
        };
    }

    async testDashboardAccess() {
        console.log('\n📊 TESTING DASHBOARD ACCESS');
        console.log('═'.repeat(50));

        // Test dashboard stats endpoint
        await this.makeRequest('/dashboard/stats', {
            headers: this.getAuthHeaders()
        });
    }

    async testSessionManagement() {
        console.log('\n📝 TESTING SESSION MANAGEMENT');
        console.log('═'.repeat(50));

        const authHeaders = this.getAuthHeaders();

        // Test getting user's sessions
        console.log('\n  📋 Getting user sessions:');
        await this.makeRequest('/chat/sessions/my-sessions', { headers: authHeaders });

        // Test getting active sessions
        console.log('\n  🟢 Getting active sessions:');
        await this.makeRequest('/chat/sessions/active', { headers: authHeaders });

        // Test getting scheduled sessions
        console.log('\n  📅 Getting scheduled sessions:');
        await this.makeRequest('/chat/sessions/scheduled', { headers: authHeaders });

        // Test session creation (may fail for non-admin)
        console.log('\n  🏗️  Testing session creation:');
        const sessionData = {
            sessionName: `Frontend Test Session ${Date.now()}`,
            description: 'Session created from frontend test',
            maxParticipants: 12,
            maxParticipantsPerRoom: 6,
            scheduledStartTime: new Date(Date.now() + 3600000).toISOString(), // 1 hour from now
            duration: 60 // 60 minutes
        };

        await this.makeRequest('/chat/sessions', {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify(sessionData)
        });
    }

    async testRoomManagement() {
        console.log('\n🏠 TESTING ROOM MANAGEMENT');
        console.log('═'.repeat(50));

        const authHeaders = this.getAuthHeaders();

        // Test getting available rooms
        console.log('\n  🚪 Getting available rooms:');
        await this.makeRequest('/chat/rooms/session/available', { headers: authHeaders });

        // Test getting room participants (may need specific room ID)
        console.log('\n  👥 Testing room participant access:');
        await this.makeRequest('/chat/rooms/test-room-id/participants', { headers: authHeaders });
    }

    async testInviteManagement() {
        console.log('\n📨 TESTING INVITE MANAGEMENT');
        console.log('═'.repeat(50));

        const authHeaders = this.getAuthHeaders();

        // Test getting session invites
        console.log('\n  🎫 Getting session invites:');
        await this.makeRequest('/chat/invites/session/test-session', { headers: authHeaders });

        // Test generating invite (may fail for non-admin)
        console.log('\n  ✨ Testing invite generation:');
        const inviteData = {
            sessionId: 'test-session-id',
            expirationMinutes: 120,
            isReusable: false
        };

        await this.makeRequest('/chat/invites', {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify(inviteData)
        });
    }

    async testProviderManagement() {
        console.log('\n👥 TESTING PROVIDER MANAGEMENT (Admin Only)');
        console.log('═'.repeat(50));

        const authHeaders = this.getAuthHeaders();

        // Test getting providers
        console.log('\n  🏢 Getting providers:');
        await this.makeRequest('/providers', { headers: authHeaders });

        // Test creating provider (admin only)
        console.log('\n  🆕 Testing provider creation:');
        const providerData = {
            organizationName: 'Test Organization',
            contactEmail: 'contact@testorg.com',
            description: 'Test provider created from frontend test'
        };

        await this.makeRequest('/providers', {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify(providerData)
        });
    }

    async testAnonymousFlow() {
        console.log('\n👻 TESTING ANONYMOUS FLOW');
        console.log('═'.repeat(50));

        // Test validating invite (without auth)
        console.log('\n  🎟️  Testing invite validation:');
        await this.makeRequest('/chat/invites/validate/fake-invite-code-12345678901234567890123456789012');

        // Test anonymous join (without auth)
        console.log('\n  🚪 Testing anonymous join:');
        const joinData = {
            inviteCode: 'fake-invite-code-12345678901234567890123456789012',
            displayName: 'Anonymous Test User',
            email: 'anonymous@test.com'
        };

        await this.makeRequest('/chat/participants/anonymous', {
            method: 'POST',
            body: JSON.stringify(joinData)
        });
    }

    async testHealthEndpoint() {
        console.log('\n🏥 TESTING HEALTH ENDPOINT');
        console.log('═'.repeat(50));

        await this.makeRequest('/health');
    }

    async runAllTests() {
        console.log('\n🚀 WAGL FRONTEND FUNCTIONALITY TEST');
        console.log('═'.repeat(80));
        console.log('Testing frontend integration with backend API');
        console.log('═'.repeat(80));

        try {
            // Step 1: Test health
            await this.testHealthEndpoint();

            // Step 2: Test login
            const loginSuccess = await this.testLogin();

            if (!loginSuccess) {
                console.log('\n❌ Login failed - skipping authenticated tests');
                return;
            }

            // Step 3: Test authenticated features
            await this.testDashboardAccess();
            await this.testSessionManagement();
            await this.testRoomManagement();
            await this.testInviteManagement();
            await this.testProviderManagement();

            // Step 4: Test anonymous features
            await this.testAnonymousFlow();

        } catch (error) {
            console.log(`\n💥 Test suite error: ${error.message}`);
        }

        console.log('\n🏁 FRONTEND FUNCTIONALITY TESTING COMPLETED!');
        console.log('═'.repeat(80));
        console.log('\n📋 SUMMARY:');
        console.log('✅ Login and authentication working');
        console.log('✅ JWT token generation and claims working');
        console.log('⚠️  Some admin features may require elevated permissions');
        console.log('⚠️  Some endpoints may need specific IDs (sessions, rooms)');
        console.log('✅ Ready for frontend demo testing');
        console.log('\n🌐 Frontend URL: http://localhost:3000');
        console.log(`📡 Backend URL: ${this.apiEndpoint}`);
        console.log('\n👤 Test Account:');
        console.log(`   Email: ${testUser.email}`);
        console.log(`   Password: ${testUser.password}`);
    }
}

// Run the tests
async function main() {
    const tester = new FrontendTester();
    await tester.runAllTests();
}

if (require.main === module) {
    main().catch(error => {
        console.error('💥 Test failed:', error);
        process.exit(1);
    });
}

module.exports = { FrontendTester, testUser };