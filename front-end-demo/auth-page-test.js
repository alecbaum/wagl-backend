const { chromium } = require('playwright');

async function testAuthPage() {
    const browser = await chromium.launch({
        headless: false,
        devtools: false,
        slowMo: 1500  // Slow down to see what's happening
    });

    const context = await browser.newContext();
    const page = await context.newPage();

    // Track network requests
    page.on('request', request => {
        const url = request.url();
        if (url.includes('register') || url.includes('login') || url.includes('api.wagl.ai')) {
            console.log(`\n🌐 REQUEST: ${request.method()} ${url}`);
            if (request.postData()) {
                console.log(`📝 BODY: ${request.postData()}`);
            }
        }
    });

    page.on('response', response => {
        const url = response.url();
        if (url.includes('register') || url.includes('login') || url.includes('api.wagl.ai')) {
            console.log(`📥 RESPONSE: ${response.status()} ${url}`);
        }
    });

    // Track console errors
    page.on('console', msg => {
        const text = msg.text();
        if (text.includes('ERROR') || text.includes('Failed') || text.includes('SSL') || text.includes('certificate') || text.includes('CERT')) {
            console.log(`⚠️  BROWSER: ${text}`);
        }
    });

    try {
        console.log('🚀 Loading frontend with auth page as default...\n');

        await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });

        // Wait for app initialization
        await page.waitForFunction(() => window.APP_STATE && window.authManager, { timeout: 10000 });

        console.log('✅ App loaded successfully\n');

        // Check what page we're on
        const currentState = await page.evaluate(() => ({
            currentPage: window.APP_STATE?.ui?.currentPage,
            currentTab: window.APP_STATE?.ui?.currentTab,
            apiEndpoint: window.APP_STATE?.apiEndpoint,
            isAuthenticated: !!window.APP_STATE?.currentUser
        }));

        console.log('📋 Current State:', currentState);

        // Check if we're on the auth page
        if (currentState.currentPage === 'auth') {
            console.log('✅ Auth page is showing by default\n');
        } else {
            console.log('❌ Not on auth page, current page:', currentState.currentPage);
            console.log('🔄 Navigating to auth page...\n');

            await page.evaluate(() => {
                if (window.pageManager) {
                    window.pageManager.showPage('auth');
                }
            });

            await page.waitForTimeout(1000);
        }

        // Check if register tab is active or switch to it
        console.log('🔄 Ensuring register tab is active...\n');

        const registerTab = await page.$('[data-tab="register"]');
        if (registerTab) {
            const isActive = await registerTab.evaluate(el => el.classList.contains('active'));
            if (!isActive) {
                console.log('🔄 Clicking register tab...');
                await registerTab.click();
                await page.waitForTimeout(1000);
            }
            console.log('✅ Register tab is active');
        } else {
            console.log('❌ Register tab not found');
        }

        // Check form visibility
        console.log('\n🔍 Checking form elements...\n');

        const formElements = await page.evaluate(() => {
            const form = document.getElementById('register-form');
            const fields = [
                'register-firstname',
                'register-lastname',
                'register-email',
                'register-password',
                'register-confirm-password'
            ];

            const results = {
                formExists: !!form,
                formVisible: form ? form.offsetParent !== null : false,
                fields: {}
            };

            fields.forEach(fieldId => {
                const element = document.getElementById(fieldId);
                results.fields[fieldId] = {
                    exists: !!element,
                    visible: element ? element.offsetParent !== null : false,
                    value: element ? element.value : null
                };
            });

            return results;
        });

        console.log('📊 Form Elements Status:');
        console.log('   Form exists:', formElements.formExists);
        console.log('   Form visible:', formElements.formVisible);
        console.log('   Fields:');
        Object.entries(formElements.fields).forEach(([fieldId, status]) => {
            const icon = status.exists && status.visible ? '✅' : '❌';
            console.log(`      ${icon} ${fieldId}: exists=${status.exists}, visible=${status.visible}`);
        });

        if (formElements.formExists && formElements.formVisible) {
            console.log('\n🎯 Attempting registration...\n');

            // Generate unique email
            const timestamp = Date.now();
            const testEmail = `test${timestamp}@example.com`;

            console.log(`📧 Using email: ${testEmail}`);

            // Fill form
            try {
                await page.fill('#register-firstname', 'TestUser');
                await page.fill('#register-lastname', 'Demo');
                await page.fill('#register-email', testEmail);
                await page.fill('#register-password', 'password123');
                await page.fill('#register-confirm-password', 'password123');

                console.log('✅ Form filled successfully');

                // Submit form
                const submitButton = await page.$('#register-form button[type="submit"], #register-form button');
                if (submitButton) {
                    const buttonText = await submitButton.textContent();
                    console.log(`\n🚀 Clicking submit button: "${buttonText}"`);

                    // Click and wait for response
                    await submitButton.click();

                    console.log('⏳ Waiting for registration response...');

                    // Wait for network activity or state change
                    await page.waitForTimeout(8000);

                    // Check result
                    const registrationResult = await page.evaluate(() => ({
                        currentUser: window.APP_STATE?.currentUser,
                        hasToken: !!window.APP_STATE?.tokens?.accessToken,
                        currentPage: window.APP_STATE?.ui?.currentPage,
                        isAuthenticated: !!window.authManager?.isAuthenticated()
                    }));

                    console.log('\n📋 Registration Result:', registrationResult);

                    if (registrationResult.hasToken && registrationResult.isAuthenticated) {
                        console.log('🎉 REGISTRATION SUCCESSFUL!');

                        // Test logout and login
                        console.log('\n🔄 Testing login functionality...');

                        // Logout first
                        await page.evaluate(() => {
                            if (window.authManager) {
                                window.authManager.logout();
                            }
                        });

                        await page.waitForTimeout(2000);

                        // Should be back on auth page
                        const loginTab = await page.$('[data-tab="login"]');
                        if (loginTab) {
                            await loginTab.click();
                            await page.waitForTimeout(1000);

                            // Fill login form
                            await page.fill('#login-email', testEmail);
                            await page.fill('#login-password', 'password123');

                            const loginButton = await page.$('#login-form button[type="submit"], #login-form button');
                            if (loginButton) {
                                console.log('🔑 Attempting login...');
                                await loginButton.click();
                                await page.waitForTimeout(5000);

                                const loginResult = await page.evaluate(() => ({
                                    hasToken: !!window.APP_STATE?.tokens?.accessToken,
                                    isAuthenticated: !!window.authManager?.isAuthenticated(),
                                    currentPage: window.APP_STATE?.ui?.currentPage
                                }));

                                console.log('📋 Login Result:', loginResult);

                                if (loginResult.hasToken && loginResult.isAuthenticated) {
                                    console.log('🎉 LOGIN SUCCESSFUL!');
                                } else {
                                    console.log('❌ LOGIN FAILED');
                                }
                            }
                        }

                    } else {
                        console.log('❌ REGISTRATION FAILED');
                    }

                } else {
                    console.log('❌ Submit button not found');
                }

            } catch (fillError) {
                console.log('❌ Error filling form:', fillError.message);
            }

        } else {
            console.log('❌ Form not ready for testing');
        }

        console.log('\n' + '='.repeat(50));
        console.log('🏁 Test completed!');
        console.log('='.repeat(50));

    } catch (error) {
        console.error('💥 Test failed:', error.message);
    }

    console.log('\n⏸️  Press Enter to close browser...');
    await new Promise(resolve => {
        process.stdin.once('data', resolve);
    });

    await browser.close();
}

testAuthPage();