const { chromium } = require('playwright');

async function completeAuthTest() {
    const browser = await chromium.launch({
        headless: false,
        devtools: true,
        slowMo: 1000,  // Slow down to see what happens
        args: ['--disable-web-security', '--disable-features=VizDisplayCompositor'] // Disable some security for testing
    });

    const context = await browser.newContext({
        ignoreHTTPSErrors: true // Ignore SSL issues
    });

    const page = await context.newPage();

    // Comprehensive logging
    page.on('request', request => {
        const url = request.url();
        if (url.includes('register') || url.includes('login') || url.includes('api.wagl.ai')) {
            console.log(`🌐 REQUEST: ${request.method()} ${url}`);
            if (request.postData()) {
                console.log(`   BODY: ${request.postData()}`);
            }
        }
    });

    page.on('response', async response => {
        const url = response.url();
        if (url.includes('register') || url.includes('login') || url.includes('api.wagl.ai')) {
            console.log(`📥 RESPONSE: ${response.status()} ${url}`);

            if (response.status() !== 200 && response.status() !== 201) {
                try {
                    const body = await response.text();
                    console.log(`   ERROR BODY: ${body}`);
                } catch (e) {
                    console.log(`   Could not read response body`);
                }
            }
        }
    });

    page.on('console', msg => {
        const text = msg.text();
        if (text.includes('ERROR') || text.includes('Failed') || text.includes('SSL') || text.includes('certificate')) {
            console.log(`⚠️  CONSOLE: ${text}`);
        }
    });

    page.on('pageerror', error => {
        console.log(`❌ PAGE ERROR: ${error.message}`);
    });

    try {
        console.log('\n🚀 Starting Complete Authentication Test');
        console.log('====================================================');

        console.log('\n1. Loading frontend demo...');
        await page.goto('http://localhost:3000', { waitUntil: 'networkidle', timeout: 30000 });

        // Wait for app to initialize
        await page.waitForFunction(() => window.APP_STATE && window.authManager, { timeout: 10000 });

        console.log('\n2. Checking initial app state...');
        const appState = await page.evaluate(() => ({
            apiEndpoint: window.APP_STATE?.apiEndpoint,
            currentPage: window.APP_STATE?.ui?.currentPage,
            isAuthenticated: !!window.APP_STATE?.currentUser,
            authManagerExists: !!window.authManager
        }));
        console.log('App State:', appState);

        console.log('\n3. Navigating to authentication page...');

        // Look for any button that might take us to auth
        const navButtons = await page.$$('button, a, [data-page], [onclick]');
        console.log(`Found ${navButtons.length} clickable elements`);

        // Try to find login/auth navigation
        let authNavFound = false;
        for (let i = 0; i < navButtons.length; i++) {
            const button = navButtons[i];
            const text = await button.textContent();
            const id = await button.getAttribute('id');
            const dataPage = await button.getAttribute('data-page');

            console.log(`   Button ${i}: "${text}" (id: ${id}, data-page: ${dataPage})`);

            if (text && (text.includes('Login') || text.includes('Register') || text.includes('Auth')) ||
                dataPage === 'auth' || id === 'show-auth') {
                console.log(`   ✅ Clicking auth navigation: "${text}"`);
                try {
                    await button.click();
                    authNavFound = true;
                    break;
                } catch (e) {
                    console.log(`   ❌ Could not click: ${e.message}`);
                }
            }
        }

        if (!authNavFound) {
            console.log('   ⚠️  No auth navigation found, trying manual navigation...');

            // Try to navigate manually via JavaScript
            await page.evaluate(() => {
                if (window.APP_STATE) {
                    window.APP_STATE.ui.currentPage = 'auth';
                    window.APP_STATE.ui.currentTab = 'register';
                }

                // Force show auth page
                const pages = document.querySelectorAll('.page');
                pages.forEach(p => p.style.display = 'none');

                const authPage = document.getElementById('auth-page');
                if (authPage) {
                    authPage.style.display = 'block';
                }

                return 'Auth page shown manually';
            });
        }

        await page.waitForTimeout(2000);

        console.log('\n4. Looking for register tab...');

        // Find and click register tab
        const registerTab = await page.$('[data-tab="register"]');
        if (registerTab) {
            console.log('   ✅ Found register tab, clicking...');
            await registerTab.click();
        } else {
            console.log('   ⚠️  Register tab not found, checking available tabs...');
            const tabs = await page.$$('[data-tab]');
            console.log(`   Found ${tabs.length} tabs:`);
            for (let tab of tabs) {
                const tabName = await tab.getAttribute('data-tab');
                const text = await tab.textContent();
                const visible = await tab.isVisible();
                console.log(`      - ${tabName}: "${text}" (visible: ${visible})`);
            }
        }

        await page.waitForTimeout(1000);

        console.log('\n5. Locating registration form...');

        // Check if form exists and is visible
        const formExists = await page.$('#register-form');
        if (!formExists) {
            console.log('   ❌ Registration form not found!');

            // Check what forms do exist
            const forms = await page.$$('form');
            console.log(`   Found ${forms.length} forms:`);
            for (let i = 0; i < forms.length; i++) {
                const form = forms[i];
                const id = await form.getAttribute('id');
                const visible = await form.isVisible();
                console.log(`      Form ${i}: id="${id}" visible=${visible}`);
            }
        } else {
            const formVisible = await formExists.isVisible();
            console.log(`   ✅ Registration form found (visible: ${formVisible})`);

            if (!formVisible) {
                // Try to make it visible
                await page.evaluate(() => {
                    const form = document.getElementById('register-form');
                    if (form) {
                        form.style.display = 'block';
                        form.style.visibility = 'visible';

                        // Also show parent containers
                        let parent = form.parentElement;
                        while (parent && parent !== document.body) {
                            parent.style.display = 'block';
                            parent.style.visibility = 'visible';
                            parent = parent.parentElement;
                        }
                    }
                });

                console.log('   ✅ Form made visible via JavaScript');
            }
        }

        console.log('\n6. Checking form fields...');

        // Check each field
        const fields = [
            { id: 'register-firstname', name: 'First Name' },
            { id: 'register-lastname', name: 'Last Name' },
            { id: 'register-email', name: 'Email' },
            { id: 'register-password', name: 'Password' },
            { id: 'register-confirm-password', name: 'Confirm Password' }
        ];

        for (let field of fields) {
            const element = await page.$(`#${field.id}`);
            if (element) {
                const visible = await element.isVisible();
                console.log(`   ✅ ${field.name} field found (visible: ${visible})`);
            } else {
                console.log(`   ❌ ${field.name} field (#${field.id}) not found!`);
            }
        }

        console.log('\n7. Filling registration form...');

        const uniqueEmail = `test-${Date.now()}@example.com`;
        console.log(`   Using email: ${uniqueEmail}`);

        try {
            await page.fill('#register-firstname', 'Playwright');
            console.log('   ✅ First name filled');

            await page.fill('#register-lastname', 'Test');
            console.log('   ✅ Last name filled');

            await page.fill('#register-email', uniqueEmail);
            console.log('   ✅ Email filled');

            await page.fill('#register-password', 'password123');
            console.log('   ✅ Password filled');

            await page.fill('#register-confirm-password', 'password123');
            console.log('   ✅ Confirm password filled');

            // Check tier selection
            const tierSelect = await page.$('#register-tier');
            if (tierSelect) {
                await page.selectOption('#register-tier', '1');
                console.log('   ✅ Tier selected');
            }

        } catch (fillError) {
            console.log(`   ❌ Error filling form: ${fillError.message}`);

            // Try alternative approach - direct value setting
            console.log('   🔄 Trying direct value setting...');
            await page.evaluate((email) => {
                const fields = [
                    { id: 'register-firstname', value: 'Playwright' },
                    { id: 'register-lastname', value: 'Test' },
                    { id: 'register-email', value: email },
                    { id: 'register-password', value: 'password123' },
                    { id: 'register-confirm-password', value: 'password123' }
                ];

                fields.forEach(field => {
                    const element = document.getElementById(field.id);
                    if (element) {
                        element.value = field.value;
                        element.dispatchEvent(new Event('input', { bubbles: true }));
                        element.dispatchEvent(new Event('change', { bubbles: true }));
                        console.log(`Set ${field.id} = ${field.value}`);
                    } else {
                        console.log(`Field ${field.id} not found`);
                    }
                });
            }, uniqueEmail);
        }

        console.log('\n8. Submitting registration form...');

        // Find submit button
        const submitBtn = await page.$('#register-form button[type="submit"], #register-form button:last-child, #register-form input[type="submit"]');
        if (submitBtn) {
            const btnText = await submitBtn.textContent();
            console.log(`   ✅ Found submit button: "${btnText}"`);

            // Clear any previous network logs
            console.log('\n   🌐 Submitting form - watching for network requests...');

            await submitBtn.click();

            console.log('   ⏳ Waiting for registration response...');

            // Wait for any registration response
            await page.waitForTimeout(8000);

        } else {
            console.log('   ❌ Submit button not found!');

            // Try form submission via JavaScript
            console.log('   🔄 Trying JavaScript form submission...');
            await page.evaluate(() => {
                const form = document.getElementById('register-form');
                if (form) {
                    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
                    return 'Form submitted via JavaScript';
                } else {
                    return 'Form not found for JavaScript submission';
                }
            });

            await page.waitForTimeout(8000);
        }

        console.log('\n9. Checking registration result...');

        const postRegState = await page.evaluate(() => ({
            currentUser: window.APP_STATE?.currentUser,
            accessToken: window.APP_STATE?.tokens?.accessToken ? 'Present' : 'None',
            currentPage: window.APP_STATE?.ui?.currentPage,
            currentTab: window.APP_STATE?.ui?.currentTab,
            authManagerExists: !!window.authManager
        }));

        console.log('Post-registration state:', postRegState);

        if (postRegState.accessToken === 'Present') {
            console.log('   ✅ REGISTRATION SUCCESS - User is authenticated!');

            // Now test login functionality
            console.log('\n10. Testing login functionality...');

            // First logout to test login
            await page.evaluate(() => {
                if (window.authManager && window.authManager.logout) {
                    window.authManager.logout();
                }
                // Clear tokens
                if (window.APP_STATE) {
                    window.APP_STATE.currentUser = null;
                    window.APP_STATE.tokens.accessToken = null;
                    window.APP_STATE.tokens.refreshToken = null;
                }
            });

            console.log('   🔓 Logged out for login test');

            // Switch to login tab
            const loginTab = await page.$('[data-tab="login"]');
            if (loginTab) {
                await loginTab.click();
                console.log('   ✅ Switched to login tab');

                await page.waitForTimeout(1000);

                // Fill login form
                try {
                    await page.fill('#login-email', uniqueEmail);
                    await page.fill('#login-password', 'password123');
                    console.log('   ✅ Login form filled');

                    // Submit login
                    const loginBtn = await page.$('#login-form button[type="submit"], #login-form button:last-child');
                    if (loginBtn) {
                        console.log('   🔄 Submitting login...');
                        await loginBtn.click();

                        await page.waitForTimeout(5000);

                        const loginResult = await page.evaluate(() => ({
                            currentUser: window.APP_STATE?.currentUser,
                            accessToken: window.APP_STATE?.tokens?.accessToken ? 'Present' : 'None'
                        }));

                        console.log('Login result:', loginResult);

                        if (loginResult.accessToken === 'Present') {
                            console.log('   ✅ LOGIN SUCCESS!');
                        } else {
                            console.log('   ❌ Login failed');
                        }

                    } else {
                        console.log('   ❌ Login submit button not found');
                    }

                } catch (loginError) {
                    console.log(`   ❌ Login form error: ${loginError.message}`);
                }
            } else {
                console.log('   ❌ Login tab not found');
            }

        } else {
            console.log('   ❌ REGISTRATION FAILED - No access token received');
        }

        console.log('\n====================================================');
        console.log('🏁 Test completed!');
        console.log('====================================================');

    } catch (error) {
        console.error(`\n💥 Test failed: ${error.message}`);
        console.error(error.stack);
    }

    console.log('\n⏸️  Test finished. Press Enter to close browser...');
    await new Promise(resolve => {
        process.stdin.once('data', resolve);
    });

    await browser.close();
}

completeAuthTest();