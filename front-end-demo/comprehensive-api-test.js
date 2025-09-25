const { chromium } = require('playwright');

// Test accounts from seeder
const testAccounts = {
    admin: {
        email: 'admin@wagl.com',
        password: 'AdminPass123!',
        expectedRoles: ['Tier3', 'Admin'],
        description: 'Admin - Can create sessions, full access'
    },
    moderator: {
        email: 'moderator@wagl.com',
        password: 'ModeratorPass123!',
        expectedRoles: ['Tier2', 'ChatModerator'],
        description: 'Moderator - Can read/write any chat room'
    },
    regularUser: {
        email: 'tier2@wagl.com',
        password: 'Tier2Pass123!',
        expectedRoles: ['Tier2'],
        description: 'Regular User - Limited access'
    }
};

class ApiTester {
    constructor(apiEndpoint = 'https://v6uwnty3vi.us-east-1.awsapprunner.com') {
        this.apiEndpoint = apiEndpoint;
        this.tokens = {};
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

        console.log(`    📡 ${config.method} ${endpoint}`);

        try {
            const response = await fetch(url, config);
            const status = response.status;
            const statusIcon = status < 300 ? '✅' : status < 500 ? '⚠️' : '❌';

            // Clone response to avoid "Body has already been read" error
            const responseClone = response.clone();
            let data;
            try {
                data = await response.json();
            } catch {
                try {
                    data = await responseClone.text();
                } catch {
                    data = 'Unable to read response';
                }
            }

            console.log(`       ${statusIcon} ${status} ${response.statusText}`);

            if (status >= 400) {
                console.log(`       📄 Response: ${JSON.stringify(data).substring(0, 100)}...`);
            }

            return { status, data, ok: response.ok };
        } catch (error) {
            console.log(`       💥 Error: ${error.message}`);
            return { status: 0, data: null, ok: false, error: error.message };
        }
    }

    async login(account) {
        console.log(`\n🔐 Logging in as ${account.description}`);
        console.log(`   📧 ${account.email}`);

        const response = await this.makeRequest('/auth/login', {
            method: 'POST',
            body: JSON.stringify({
                email: account.email,
                password: account.password
            })
        });

        if (response.ok && response.data.accessToken) {
            this.tokens[account.email] = response.data.accessToken;
            console.log(`   ✅ Login successful`);
            console.log(`   👤 User: ${response.data.user.fullName}`);

            // Decode JWT to see roles
            try {
                const payload = JSON.parse(atob(response.data.accessToken.split('.')[1]));
                console.log(`   🎭 JWT Claims:`, {
                    sub: payload.sub,
                    email: payload.email,
                    role: payload.role,
                    account_type: payload.account_type,
                    tier_level: payload.tier_level
                });
            } catch (e) {
                console.log(`   ⚠️ Could not decode JWT`);
            }

            return true;
        } else {
            console.log(`   ❌ Login failed: ${response.data?.message || 'Unknown error'}`);
            return false;
        }
    }

    getAuthHeaders(email) {
        const token = this.tokens[email];
        if (!token) {
            throw new Error(`No token found for ${email}`);
        }
        return {
            'Authorization': `Bearer ${token}`
        };
    }

    async testUserApis(account) {
        console.log(`\n🧪 Testing APIs for ${account.description}`);
        console.log('=' .repeat(60));

        const email = account.email;
        const authHeaders = this.getAuthHeaders(email);

        // Test 1: Dashboard Stats
        console.log('\n📊 Dashboard APIs:');
        await this.makeRequest('/dashboard/stats', { headers: authHeaders });

        // Test 2: Session Management
        console.log('\n📝 Session Management APIs:');
        await this.makeRequest('/chat/sessions/my-sessions', { headers: authHeaders });
        await this.makeRequest('/chat/sessions/active', { headers: authHeaders });
        await this.makeRequest('/chat/sessions/scheduled', { headers: authHeaders });

        // Test 3: Session Creation (Admin only)
        console.log('\n🏗️  Session Creation (Admin Only):');
        const sessionData = {
            sessionName: `Test Session ${Date.now()}`,
            description: 'API test session',
            maxParticipants: 10,
            isPrivate: false
        };

        await this.makeRequest('/chat/sessions', {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify(sessionData)
        });

        // Test 4: Room Management
        console.log('\n🏠 Room Management APIs:');
        // Note: These might need sessionId, but let's test the endpoints
        await this.makeRequest('/chat/rooms/session/available', { headers: authHeaders });

        // Test 5: Invite Management
        console.log('\n📨 Invite Management APIs:');
        // Test getting invites (might return empty)
        await this.makeRequest('/chat/invites/session/test', { headers: authHeaders });

        // Test 6: Provider Management (Admin only)
        console.log('\n👥 Provider Management (Admin Only):');
        await this.makeRequest('/providers', { headers: authHeaders });

        console.log('\n' + '=' .repeat(60));
    }

    async testAnonymousApis() {
        console.log(`\n👻 Testing Anonymous APIs`);
        console.log('=' .repeat(60));

        // Test anonymous endpoints (no auth)
        await this.makeRequest('/health'); // This should work without auth

        // Test protected endpoints without auth (should fail)
        console.log('\n🚫 Testing Protected Endpoints Without Auth (Should Fail):');
        await this.makeRequest('/dashboard/stats'); // Should return 401
        await this.makeRequest('/chat/sessions/my-sessions'); // Should return 401

        console.log('\n' + '=' .repeat(60));
    }
}

async function runComprehensiveApiTests() {
    console.log('\n🚀 COMPREHENSIVE API TESTING');
    console.log('=' .repeat(80));
    console.log('Testing all user roles and API endpoints');
    console.log('=' .repeat(80));

    const tester = new ApiTester();

    // Test 1: Anonymous access
    await tester.testAnonymousApis();

    // Test 2: Login and test each user type
    for (const [userType, account] of Object.entries(testAccounts)) {
        try {
            const loginSuccess = await tester.login(account);
            if (loginSuccess) {
                await tester.testUserApis(account);
            } else {
                console.log(`❌ Skipping API tests for ${account.description} due to login failure`);
            }
        } catch (error) {
            console.log(`💥 Error testing ${account.description}:`, error.message);
        }
    }

    // Test 3: Permission Matrix Summary
    console.log('\n📋 PERMISSION MATRIX SUMMARY');
    console.log('=' .repeat(80));
    console.log('👑 Admin (admin@wagl.com):');
    console.log('   ✅ Can create sessions');
    console.log('   ✅ Can join any chat room');
    console.log('   ✅ Full dashboard access');
    console.log('   ✅ Provider management');

    console.log('\n🔧 Moderator (moderator@wagl.com):');
    console.log('   ❌ Cannot create sessions');
    console.log('   ✅ Can read/write ANY chat room');
    console.log('   ✅ Basic dashboard access');
    console.log('   ❌ No provider management');

    console.log('\n👤 Regular User (tier2@wagl.com):');
    console.log('   ❌ Cannot create sessions');
    console.log('   ❌ Can only join invited sessions');
    console.log('   ✅ Basic dashboard access');
    console.log('   ❌ No provider management');

    console.log('\n👻 Anonymous:');
    console.log('   ❌ No account required');
    console.log('   ❌ Join via invite link only');
    console.log('   ❌ No API access');

    console.log('\n' + '=' .repeat(80));
    console.log('🏁 COMPREHENSIVE API TESTING COMPLETED!');
    console.log('=' .repeat(80));
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ApiTester, testAccounts };
}

// Run if called directly
if (require.main === module) {
    runComprehensiveApiTests().catch(error => {
        console.error('💥 Test suite failed:', error);
        process.exit(1);
    });
}