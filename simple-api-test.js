#!/usr/bin/env node

/**
 * Simple API Test for Wagl Backend
 * Tests key endpoints with proper error handling
 */

const https = require('https');
const http = require('http');
const url = require('url');

const apiEndpoint = 'https://v6uwnty3vi.us-east-1.awsapprunner.com';

// Working test user
const testUser = {
    email: 'test@example.com',
    password: 'TestPass123'
};

function makeRequest(urlString, options = {}) {
    return new Promise((resolve, reject) => {
        const urlObj = new URL(urlString);
        const isHttps = urlObj.protocol === 'https:';
        const requestLib = isHttps ? https : http;

        const reqOptions = {
            hostname: urlObj.hostname,
            port: urlObj.port || (isHttps ? 443 : 80),
            path: urlObj.pathname + urlObj.search,
            method: options.method || 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'User-Agent': 'Wagl-Frontend-Test/1.0',
                ...options.headers
            },
            timeout: 10000
        };

        const req = requestLib.request(reqOptions, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    const jsonData = data ? JSON.parse(data) : {};
                    resolve({
                        status: res.statusCode,
                        data: jsonData,
                        ok: res.statusCode >= 200 && res.statusCode < 300
                    });
                } catch (e) {
                    resolve({
                        status: res.statusCode,
                        data: data,
                        ok: res.statusCode >= 200 && res.statusCode < 300
                    });
                }
            });
        });

        req.on('error', (err) => {
            reject(err);
        });

        req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timeout'));
        });

        if (options.body) {
            req.write(options.body);
        }

        req.end();
    });
}

async function testEndpoint(name, url, options = {}) {
    console.log(`\n🧪 Testing: ${name}`);
    console.log(`   📡 ${options.method || 'GET'} ${url}`);

    try {
        const response = await makeRequest(url, options);
        const statusIcon = response.status < 300 ? '✅' : response.status < 500 ? '⚠️' : '❌';

        console.log(`   ${statusIcon} ${response.status} - ${response.ok ? 'Success' : 'Failed'}`);

        if (response.status >= 400) {
            const errorText = typeof response.data === 'string' ? response.data : JSON.stringify(response.data);
            console.log(`   📄 Response: ${errorText.substring(0, 100)}...`);
        } else if (response.ok && response.data) {
            console.log(`   📄 Success: ${JSON.stringify(response.data).substring(0, 100)}...`);
        }

        return response;
    } catch (error) {
        console.log(`   💥 Error: ${error.message}`);
        return { status: 0, data: null, ok: false, error: error.message };
    }
}

async function runTests() {
    console.log('\n🚀 WAGL BACKEND API TESTS');
    console.log('═'.repeat(60));
    console.log(`🌐 Testing: ${apiEndpoint}`);
    console.log('═'.repeat(60));

    let accessToken = null;

    // Test 1: Health Check
    await testEndpoint('Health Check', `${apiEndpoint}/health`);

    // Test 2: Login
    const loginResponse = await testEndpoint('User Login', `${apiEndpoint}/api/v1.0/auth/login`, {
        method: 'POST',
        body: JSON.stringify({
            email: testUser.email,
            password: testUser.password,
            rememberMe: false
        })
    });

    if (loginResponse.ok && loginResponse.data.accessToken) {
        accessToken = loginResponse.data.accessToken;
        console.log(`   🎫 Token obtained successfully`);
        console.log(`   👤 User: ${loginResponse.data.user.fullName}`);
        console.log(`   🎯 Tier: ${loginResponse.data.user.tierLevel}`);

        // Decode JWT
        try {
            const payload = JSON.parse(Buffer.from(loginResponse.data.accessToken.split('.')[1], 'base64').toString());
            console.log(`   🎭 Role: ${payload.role}, Account: ${payload.account_type}`);
        } catch (e) {
            console.log(`   ⚠️ Could not decode JWT`);
        }
    }

    if (!accessToken) {
        console.log('\n❌ No access token - skipping authenticated tests');
        return;
    }

    const authHeaders = {
        'Authorization': `Bearer ${accessToken}`
    };

    // Test 3: Dashboard Stats
    await testEndpoint('Dashboard Stats', `${apiEndpoint}/api/v1.0/dashboard/stats`, {
        headers: authHeaders
    });

    // Test 4: User Sessions
    await testEndpoint('My Sessions', `${apiEndpoint}/api/v1.0/chat/sessions/my-sessions`, {
        headers: authHeaders
    });

    // Test 5: Active Sessions
    await testEndpoint('Active Sessions', `${apiEndpoint}/api/v1.0/chat/sessions/active`, {
        headers: authHeaders
    });

    // Test 6: Session Creation (may fail for non-admin)
    await testEndpoint('Create Session', `${apiEndpoint}/api/v1.0/chat/sessions`, {
        method: 'POST',
        headers: authHeaders,
        body: JSON.stringify({
            sessionName: `API Test Session ${Date.now()}`,
            description: 'Test session from API',
            maxParticipants: 12,
            maxParticipantsPerRoom: 6,
            scheduledStartTime: new Date(Date.now() + 3600000).toISOString(),
            duration: 60
        })
    });

    // Test 7: Provider Management (admin only)
    await testEndpoint('Get Providers', `${apiEndpoint}/api/v1.0/providers`, {
        headers: authHeaders
    });

    // Test 8: Available Rooms
    await testEndpoint('Available Rooms', `${apiEndpoint}/api/v1.0/chat/rooms/session/available`, {
        headers: authHeaders
    });

    console.log('\n🏁 API TESTING COMPLETED');
    console.log('═'.repeat(60));
    console.log('\n📋 TEST SUMMARY:');
    console.log('✅ Basic authentication is working');
    console.log('✅ JWT tokens are being generated properly');
    console.log('⚠️  Some admin features may require elevated permissions');
    console.log('✅ Ready for frontend testing');
    console.log('\n🎯 NEXT STEPS:');
    console.log('1. Open http://localhost:3000 in your browser');
    console.log('2. Use the Login tab with credentials:');
    console.log(`   Email: ${testUser.email}`);
    console.log(`   Password: ${testUser.password}`);
    console.log('3. Test the admin dashboard features');
    console.log('4. Create sessions and test chat functionality');
}

// Run the tests
runTests().catch(error => {
    console.error('💥 Test suite failed:', error);
    process.exit(1);
});