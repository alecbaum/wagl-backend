const { chromium } = require('playwright');

async function debugRegistration() {
    const browser = await chromium.launch({ headless: false, devtools: true });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Listen to all network requests
    const networkRequests = [];
    page.on('request', request => {
        console.log(`→ REQUEST: ${request.method()} ${request.url()}`);
        if (request.postData()) {
            console.log(`  Body: ${request.postData()}`);
        }
        networkRequests.push({
            method: request.method(),
            url: request.url(),
            headers: request.headers(),
            postData: request.postData()
        });
    });

    page.on('response', response => {
        console.log(`← RESPONSE: ${response.status()} ${response.url()}`);
    });

    // Listen to console logs from the page
    page.on('console', msg => {
        console.log(`PAGE LOG: ${msg.text()}`);
    });

    // Listen to page errors
    page.on('pageerror', error => {
        console.log(`PAGE ERROR: ${error.message}`);
    });

    try {
        console.log('Navigating to frontend demo...');
        await page.goto('http://localhost:3000');

        console.log('Waiting for page to load...');
        await page.waitForLoadState('networkidle');

        console.log('Looking for navigation buttons...');

        // Check what buttons are available
        const buttons = await page.$$eval('button', buttons =>
            buttons.map(btn => ({
                id: btn.id,
                className: btn.className,
                textContent: btn.textContent?.trim(),
                visible: !btn.hidden && btn.offsetParent !== null
            }))
        );
        console.log('Available buttons:', buttons.filter(b => b.visible));

        // Try to find the correct way to navigate to auth page
        const authButtons = await page.$$('button');
        let authFound = false;

        for (let btn of authButtons) {
            const text = await btn.textContent();
            if (text && (text.includes('Login') || text.includes('Auth') || text.includes('Register'))) {
                console.log(`Found auth button: "${text}"`);
                await btn.click();
                authFound = true;
                break;
            }
        }

        if (!authFound) {
            // Maybe the register tab is already visible
            const registerTab = await page.$('[data-tab="register"]');
            if (registerTab) {
                console.log('Register tab found directly');
            } else {
                console.log('Register tab not found, checking all tabs...');
                const tabs = await page.$$eval('[data-tab]', tabs =>
                    tabs.map(tab => ({
                        tab: tab.dataset.tab,
                        text: tab.textContent?.trim(),
                        visible: !tab.hidden && tab.offsetParent !== null
                    }))
                );
                console.log('Available tabs:', tabs);
            }
        }

        await page.waitForTimeout(2000); // Wait for navigation

        console.log('Clicking on Register tab...');
        await page.click('[data-tab="register"]');

        console.log('Waiting for register form...');
        await page.waitForSelector('#register-form', { timeout: 10000 });

        console.log('Filling out registration form...');
        await page.fill('#register-first-name', 'TestUser');
        await page.fill('#register-last-name', 'Demo');
        await page.fill('#register-email', 'testuser@example.com');
        await page.fill('#register-password', 'password123');
        await page.fill('#register-confirm-password', 'password123');

        console.log('Submitting registration form...');

        // Clear network requests array to focus on registration request
        networkRequests.length = 0;

        await page.click('#register-form button[type="submit"]');

        console.log('Waiting for network activity...');
        await page.waitForTimeout(5000); // Wait 5 seconds to capture all requests

        console.log('\n=== REGISTRATION NETWORK REQUESTS ===');
        networkRequests.forEach((req, index) => {
            console.log(`Request ${index + 1}:`);
            console.log(`  Method: ${req.method}`);
            console.log(`  URL: ${req.url}`);
            console.log(`  Headers:`, JSON.stringify(req.headers, null, 2));
            if (req.postData) {
                console.log(`  Body: ${req.postData}`);
            }
            console.log('---');
        });

        // Check current page state
        console.log('\n=== CURRENT PAGE STATE ===');
        const currentUrl = page.url();
        console.log(`Current URL: ${currentUrl}`);

        // Check for any visible errors
        const errorElements = await page.$$('.error, .toast-error, [data-testid="error"]');
        if (errorElements.length > 0) {
            console.log('Error elements found:');
            for (let error of errorElements) {
                const text = await error.textContent();
                console.log(`  Error: ${text}`);
            }
        }

        // Check APP_STATE in browser
        const appState = await page.evaluate(() => {
            return {
                apiEndpoint: window.APP_STATE?.apiEndpoint,
                currentUser: window.APP_STATE?.currentUser,
                tokens: window.APP_STATE?.tokens
            };
        });
        console.log('APP_STATE:', JSON.stringify(appState, null, 2));

        console.log('\nTest completed. Press Enter to close browser...');
        await new Promise(resolve => {
            process.stdin.once('data', resolve);
        });

    } catch (error) {
        console.error('Test failed:', error);
    } finally {
        await browser.close();
    }
}

debugRegistration();