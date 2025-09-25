const { chromium } = require('playwright');

async function finalRegistrationTest() {
    const browser = await chromium.launch({
        headless: false,
        devtools: false,
        slowMo: 500  // Slow down for better debugging
    });

    const context = await browser.newContext();
    const page = await context.newPage();

    // Network monitoring
    const networkRequests = [];
    page.on('request', request => {
        if (request.url().includes('register')) {
            console.log(`\n🌐 REGISTRATION REQUEST:`);
            console.log(`   URL: ${request.url()}`);
            console.log(`   Method: ${request.method()}`);
            console.log(`   Headers:`, request.headers());
            console.log(`   Body: ${request.postData() || 'No body'}`);
        }
        networkRequests.push(request);
    });

    page.on('response', response => {
        if (response.url().includes('register')) {
            console.log(`\n📥 REGISTRATION RESPONSE:`);
            console.log(`   Status: ${response.status()}`);
            console.log(`   Headers:`, response.headers());
        }
    });

    page.on('pageerror', error => {
        console.log(`\n❌ PAGE ERROR: ${error.message}`);
    });

    page.on('console', msg => {
        const text = msg.text();
        if (text.includes('ERROR') || text.includes('ApiClient') || text.includes('register') || text.includes('SSL') || text.includes('certificate')) {
            console.log(`\n💬 PAGE: ${text}`);
        }
    });

    try {
        console.log('🚀 Loading frontend demo...');
        await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });

        console.log('\n🔍 Checking current state...');
        const initialState = await page.evaluate(() => ({
            apiEndpoint: window.APP_STATE?.apiEndpoint,
            currentPage: window.APP_STATE?.ui?.currentPage,
            formExists: !!document.getElementById('register-form'),
            firstnameExists: !!document.getElementById('register-firstname'),
            emailExists: !!document.getElementById('register-email')
        }));
        console.log('Initial state:', initialState);

        console.log('\n🎯 Directly accessing registration via JavaScript...');

        // Use JavaScript to directly trigger registration
        const registrationResult = await page.evaluate(async () => {
            try {
                // Check if authManager exists
                if (!window.authManager) {
                    return { error: 'authManager not available' };
                }

                const testData = {
                    firstName: 'Playwright',
                    lastName: 'Test',
                    email: 'playwright@test.com',
                    password: 'password123',
                    confirmPassword: 'password123',
                    requestedTier: 1
                };

                console.log('🔄 Calling authManager.register with:', testData);

                const result = await window.authManager.register(testData);
                return { success: true, result: result };

            } catch (error) {
                return {
                    error: error.message,
                    stack: error.stack,
                    name: error.name
                };
            }
        });

        console.log('\n📋 Registration Result:', registrationResult);

        // Wait a bit to see any additional network activity
        console.log('\n⏳ Waiting for any additional network activity...');
        await page.waitForTimeout(5000);

        // Check final state
        const finalState = await page.evaluate(() => ({
            currentUser: window.APP_STATE?.currentUser,
            hasTokens: !!window.APP_STATE?.tokens?.accessToken,
            currentPage: window.APP_STATE?.ui?.currentPage
        }));
        console.log('\n✅ Final State:', finalState);

        // Show summary of all registration-related requests
        const regRequests = networkRequests.filter(req =>
            req.url().includes('register') || req.url().includes('auth')
        );

        console.log(`\n📊 Registration-related requests: ${regRequests.length}`);
        regRequests.forEach((req, i) => {
            console.log(`   ${i+1}. ${req.method()} ${req.url()}`);
        });

    } catch (error) {
        console.error('\n💥 Test Error:', error.message);
    }

    console.log('\n✨ Test completed. Press Enter to close browser...');
    await new Promise(resolve => {
        process.stdin.once('data', resolve);
    });

    await browser.close();
}

finalRegistrationTest();