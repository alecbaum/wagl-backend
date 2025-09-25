const { chromium } = require('playwright');

async function workingFeaturesTest() {
    const browser = await chromium.launch({
        headless: false,
        devtools: false,
        slowMo: 1200
    });

    const context = await browser.newContext({
        ignoreHTTPSErrors: true
    });

    const page = await context.newPage();

    // Network monitoring
    const apiCalls = [];
    page.on('request', request => {
        if (request.url().includes('/api/v1.0/')) {
            const endpoint = request.url().split('/api/v1.0')[1];
            console.log(`🌐 ${request.method()} ${endpoint}`);
            apiCalls.push(`${request.method()} ${endpoint}`);
        }
    });

    page.on('response', response => {
        if (response.url().includes('/api/v1.0/')) {
            const endpoint = response.url().split('/api/v1.0')[1];
            const status = response.status();
            const icon = status < 300 ? '✅' : status === 404 ? '⚠️' : '❌';
            console.log(`   ${icon} ${status} ${endpoint}`);
        }
    });

    try {
        console.log('\n🧪 TESTING WORKING FRONTEND FEATURES');
        console.log('=' .repeat(50));

        // === 1. AUTHENTICATION ===
        console.log('\n1️⃣ AUTHENTICATION FLOW');
        console.log('------------------------');

        await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
        await page.waitForFunction(() => window.APP_STATE && window.authManager, { timeout: 10000 });

        const timestamp = Date.now();
        const testEmail = `fulltest${timestamp}@example.com`;

        // Register
        const registerTab = await page.$('[data-tab="register"]');
        if (registerTab) await registerTab.click();

        await page.fill('#register-firstname', 'FeatureTest');
        await page.fill('#register-lastname', 'User');
        await page.fill('#register-email', testEmail);
        await page.fill('#register-password', 'password123');
        await page.fill('#register-confirm-password', 'password123');

        const submitBtn = await page.$('#register-form button[type="submit"]');
        await submitBtn.click();
        await page.waitForTimeout(3000);

        const authState = await page.evaluate(() => ({
            authenticated: !!window.authManager?.isAuthenticated(),
            user: window.APP_STATE?.currentUser?.email,
            hasToken: !!window.APP_STATE?.tokens?.accessToken,
            currentPage: window.APP_STATE?.ui?.currentPage
        }));

        console.log('📋 Auth Result:', authState);

        if (authState.authenticated) {
            console.log('✅ Authentication: SUCCESS');
        } else {
            console.log('❌ Authentication: FAILED');
            return;
        }

        // === 2. UI NAVIGATION ===
        console.log('\n2️⃣ UI NAVIGATION TESTING');
        console.log('-------------------------');

        // Test all available pages/tabs
        const navigationElements = await page.$$('button[data-tab], button[data-page], .nav-item, .tab-btn');
        console.log(`🧭 Found ${navigationElements.length} navigation elements`);

        for (let i = 0; i < Math.min(navigationElements.length, 10); i++) {
            const element = navigationElements[i];
            try {
                const text = await element.textContent();
                const dataTab = await element.getAttribute('data-tab');
                const dataPage = await element.getAttribute('data-page');
                const isVisible = await element.isVisible();

                if (isVisible && text && text.trim()) {
                    console.log(`🔍 Testing navigation: "${text.trim()}" (tab:${dataTab}, page:${dataPage})`);

                    await element.click();
                    await page.waitForTimeout(1500);

                    const currentPage = await page.evaluate(() => window.APP_STATE?.ui?.currentPage);
                    const currentTab = await page.evaluate(() => window.APP_STATE?.ui?.currentTab);
                    console.log(`   ➜ Now on page: ${currentPage}, tab: ${currentTab}`);
                }
            } catch (navError) {
                console.log(`   ⚠️  Navigation error: ${navError.message.substring(0, 50)}`);
            }
        }

        // === 3. API ENDPOINT TESTING ===
        console.log('\n3️⃣ API ENDPOINT TESTING');
        console.log('------------------------');

        // Try API test functionality
        const apiTestElements = await page.$$('button:has-text("Test"), [data-action*="test"], #test-all-endpoints');
        console.log(`🧪 Found ${apiTestElements.length} API test elements`);

        for (let testBtn of apiTestElements) {
            try {
                const isVisible = await testBtn.isVisible();
                if (isVisible) {
                    const text = await testBtn.textContent();
                    console.log(`🧪 Clicking test button: "${text}"`);

                    await testBtn.click();
                    await page.waitForTimeout(3000);
                    break; // Only test the first visible one
                }
            } catch (testError) {
                console.log(`   ⚠️  Test button error: ${testError.message.substring(0, 50)}`);
            }
        }

        // === 4. FORM FUNCTIONALITY ===
        console.log('\n4️⃣ FORM TESTING');
        console.log('----------------');

        // Find all forms
        const forms = await page.$$('form');
        console.log(`📝 Found ${forms.length} forms on page`);

        for (let i = 0; i < forms.length; i++) {
            const form = forms[i];
            try {
                const formId = await form.getAttribute('id');
                const isVisible = await form.isVisible();

                if (isVisible && formId && !formId.includes('register') && !formId.includes('login')) {
                    console.log(`📝 Testing form: ${formId}`);

                    // Find inputs in this form
                    const inputs = await form.$$('input[type="text"], input[type="email"], textarea, select');
                    console.log(`   📋 Form has ${inputs.length} inputs`);

                    // Try to fill some inputs (safely)
                    for (let j = 0; j < Math.min(inputs.length, 3); j++) {
                        try {
                            const input = inputs[j];
                            const inputType = await input.getAttribute('type');
                            const inputName = await input.getAttribute('name');
                            const placeholder = await input.getAttribute('placeholder');

                            console.log(`      📝 Input: ${inputName || 'unnamed'} (${inputType})`);

                            // Fill with safe test data
                            if (inputType === 'email' || inputName?.includes('email')) {
                                await input.fill('test@example.com');
                            } else if (inputName?.includes('name') || placeholder?.includes('name')) {
                                await input.fill('Test Name');
                            } else {
                                await input.fill('Test Value');
                            }

                            await page.waitForTimeout(500);
                        } catch (inputError) {
                            console.log(`         ⚠️  Input error: ${inputError.message.substring(0, 30)}`);
                        }
                    }

                    // Try to submit (but don't wait long)
                    const submitBtn = await form.$('button[type="submit"], button:last-child');
                    if (submitBtn) {
                        try {
                            console.log(`   🚀 Testing form submission`);
                            await submitBtn.click();
                            await page.waitForTimeout(2000);
                        } catch (submitError) {
                            console.log(`      ⚠️  Submit error: ${submitError.message.substring(0, 30)}`);
                        }
                    }
                }
            } catch (formError) {
                console.log(`   ⚠️  Form error: ${formError.message.substring(0, 50)}`);
            }
        }

        // === 5. BUTTON INTERACTIONS ===
        console.log('\n5️⃣ INTERACTIVE ELEMENTS');
        console.log('------------------------');

        // Find action buttons (excluding navigation)
        const actionButtons = await page.$$('button:not([data-tab]):not([data-page]):not(.nav-item):not(.tab-btn)');
        console.log(`🔘 Found ${actionButtons.length} action buttons`);

        const buttonTexts = [];
        for (let btn of actionButtons.slice(0, 15)) { // Test max 15 buttons
            try {
                const isVisible = await btn.isVisible();
                const isEnabled = await btn.isEnabled();
                const text = await btn.textContent();

                if (isVisible && isEnabled && text && text.trim() &&
                    !text.includes('Register') && !text.includes('Login')) {

                    const btnText = text.trim();
                    if (!buttonTexts.includes(btnText)) {
                        buttonTexts.push(btnText);
                        console.log(`🔘 Testing button: "${btnText}"`);

                        await btn.click();
                        await page.waitForTimeout(1500);
                    }
                }
            } catch (btnError) {
                console.log(`   ⚠️  Button error: ${btnError.message.substring(0, 30)}`);
            }
        }

        // === 6. MODAL/POPUP TESTING ===
        console.log('\n6️⃣ MODAL/POPUP TESTING');
        console.log('-----------------------');

        // Look for modals
        const modals = await page.$$('.modal, .popup, .dialog, [data-modal]');
        console.log(`🪟 Found ${modals.length} modal elements`);

        // Try to trigger modals
        const modalTriggers = await page.$$('button:has-text("Create"), button:has-text("Add"), button:has-text("New")');
        for (let trigger of modalTriggers.slice(0, 3)) {
            try {
                const isVisible = await trigger.isVisible();
                if (isVisible) {
                    const text = await trigger.textContent();
                    console.log(`🪟 Triggering modal: "${text}"`);

                    await trigger.click();
                    await page.waitForTimeout(2000);

                    // Try to close modal
                    const closeBtn = await page.$('.modal .close, .popup .close, button:has-text("Close"), button:has-text("Cancel")');
                    if (closeBtn) {
                        await closeBtn.click();
                        await page.waitForTimeout(1000);
                    }
                }
            } catch (modalError) {
                console.log(`   ⚠️  Modal error: ${modalError.message.substring(0, 30)}`);
            }
        }

        // === 7. FINAL STATUS ===
        console.log('\n7️⃣ FINAL STATUS CHECK');
        console.log('----------------------');

        const finalState = await page.evaluate(() => ({
            authenticated: !!window.authManager?.isAuthenticated(),
            currentPage: window.APP_STATE?.ui?.currentPage,
            currentTab: window.APP_STATE?.ui?.currentTab,
            apiEndpoint: window.APP_STATE?.apiEndpoint,
            userEmail: window.APP_STATE?.currentUser?.email,
            hasSignalR: !!window.signalRManager,
            connectionState: window.signalRManager?.connection?.connectionState
        }));

        console.log('📊 Final Application State:', finalState);

        // === 8. API SUMMARY ===
        console.log('\n8️⃣ API ENDPOINTS TESTED');
        console.log('------------------------');

        const uniqueAPIs = [...new Set(apiCalls)];
        console.log(`📊 Total API calls: ${apiCalls.length}`);
        console.log(`🎯 Unique endpoints: ${uniqueAPIs.length}`);

        uniqueAPIs.forEach(api => {
            console.log(`   • ${api}`);
        });

        console.log('\n' + '='.repeat(50));
        console.log('✅ COMPREHENSIVE FEATURE TEST COMPLETED!');
        console.log('='.repeat(50));

        // Generate summary
        const summary = {
            authentication: finalState.authenticated ? 'WORKING' : 'FAILED',
            navigation: 'TESTED',
            forms: 'TESTED',
            buttons: 'TESTED',
            modals: 'TESTED',
            apiEndpoints: uniqueAPIs.length,
            totalRequests: apiCalls.length,
            signalR: finalState.hasSignalR ? 'AVAILABLE' : 'NOT_FOUND'
        };

        console.log('\n📋 TEST SUMMARY:');
        Object.entries(summary).forEach(([feature, status]) => {
            console.log(`   ${feature}: ${status}`);
        });

    } catch (error) {
        console.error('\n💥 Test error:', error.message);
    }

    console.log('\n⏸️  Press Enter to close browser and see final results...');
    await new Promise(resolve => {
        process.stdin.once('data', resolve);
    });

    await browser.close();
}

workingFeaturesTest();