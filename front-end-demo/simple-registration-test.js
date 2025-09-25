const { chromium } = require('playwright');

async function testRegistration() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Listen to network requests
    const requests = [];
    page.on('request', request => {
        console.log(`→ ${request.method()} ${request.url()}`);
        if (request.url().includes('register') && request.postData()) {
            console.log('REGISTRATION REQUEST BODY:', request.postData());
        }
        requests.push(request);
    });

    page.on('response', response => {
        if (response.url().includes('register')) {
            console.log(`← REGISTER RESPONSE: ${response.status()}`);
        }
    });

    // Listen to console
    page.on('console', msg => {
        if (msg.text().includes('ERROR') || msg.text().includes('register') || msg.text().includes('ApiClient')) {
            console.log(`PAGE: ${msg.text()}`);
        }
    });

    try {
        await page.goto('http://localhost:3000');
        await page.waitForLoadState('networkidle');

        console.log('\n=== FORCING REGISTRATION FORM VISIBILITY ===');

        // Force show auth page
        await page.evaluate(() => {
            if (window.APP_STATE) {
                window.APP_STATE.ui.currentPage = 'auth';
                window.APP_STATE.ui.currentTab = 'register';
            }

            // Hide other sections and show auth
            const pages = document.querySelectorAll('.page');
            pages.forEach(p => p.style.display = 'none');

            const authPage = document.getElementById('auth-page');
            if (authPage) {
                authPage.style.display = 'block';

                // Show register tab
                const tabs = authPage.querySelectorAll('.tab-content');
                tabs.forEach(t => t.style.display = 'none');

                const registerTab = document.getElementById('register-tab');
                if (registerTab) {
                    registerTab.style.display = 'block';
                }
            }

            console.log('Auth page and register tab forced visible');
        });

        await page.waitForTimeout(1000);

        console.log('\n=== FILLING REGISTRATION FORM ===');

        // Fill out the form
        await page.fill('#register-first-name', 'Test');
        await page.fill('#register-last-name', 'User');
        await page.fill('#register-email', 'uniquetest@example.com');
        await page.fill('#register-password', 'password123');
        await page.fill('#register-confirm-password', 'password123');

        console.log('\n=== SUBMITTING FORM ===');

        // Submit the form
        await page.click('#register-form button[type="submit"]');

        console.log('\n=== WAITING FOR RESPONSE ===');

        // Wait for any registration-related network activity
        await page.waitForTimeout(10000);

        console.log('\n=== FINAL STATE ===');

        // Check final state
        const finalState = await page.evaluate(() => ({
            currentPage: window.APP_STATE?.ui?.currentPage,
            currentUser: window.APP_STATE?.currentUser,
            hasTokens: !!window.APP_STATE?.tokens?.accessToken,
            endpoint: window.APP_STATE?.apiEndpoint
        }));

        console.log('Final state:', finalState);

    } catch (error) {
        console.error('Test error:', error);
    } finally {
        console.log('\nPress Enter to close...');
        await new Promise(resolve => {
            process.stdin.once('data', resolve);
        });
        await browser.close();
    }
}

testRegistration();