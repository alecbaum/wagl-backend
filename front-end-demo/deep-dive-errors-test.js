const { chromium } = require('playwright');

async function deepDiveErrorsTest() {
    const browser = await chromium.launch({
        headless: false,
        devtools: true,  // Keep dev tools open
        slowMo: 2000     // Very slow to see everything
    });

    const context = await browser.newContext({
        ignoreHTTPSErrors: true
    });

    const page = await context.newPage();

    // Comprehensive error tracking
    const errors = [];
    const networkRequests = [];

    page.on('request', request => {
        networkRequests.push({
            method: request.method(),
            url: request.url(),
            postData: request.postData(),
            headers: request.headers(),
            timestamp: new Date().toISOString()
        });

        if (request.url().includes('/api/v1.0/')) {
            const endpoint = request.url().split('/api/v1.0')[1];
            console.log(`🌐 REQUEST: ${request.method()} ${endpoint}`);
            if (request.postData()) {
                console.log(`📝 BODY: ${request.postData()}`);
            }
        }
    });

    page.on('response', async response => {
        if (response.url().includes('/api/v1.0/')) {
            const endpoint = response.url().split('/api/v1.0')[1];
            const status = response.status();

            if (status >= 400) {
                let errorBody = '';
                try {
                    errorBody = await response.text();
                } catch (e) {
                    errorBody = 'Could not read response body';
                }

                errors.push({
                    endpoint,
                    status,
                    body: errorBody,
                    url: response.url()
                });

                console.log(`❌ ${status} ${endpoint}`);
                console.log(`   ERROR: ${errorBody.substring(0, 200)}`);
            } else {
                console.log(`✅ ${status} ${endpoint}`);
            }
        }
    });

    // JavaScript errors
    page.on('pageerror', error => {
        errors.push({
            type: 'JavaScript',
            message: error.message,
            stack: error.stack
        });
        console.log(`💥 JS ERROR: ${error.message}`);
    });

    // Console errors
    page.on('console', msg => {
        if (msg.type() === 'error') {
            errors.push({
                type: 'Console',
                message: msg.text()
            });
            console.log(`🔴 CONSOLE ERROR: ${msg.text()}`);
        }
    });

    try {
        console.log('\n🔍 DEEP DIVE ERROR ANALYSIS');
        console.log('=' .repeat(60));

        // === SETUP ===
        console.log('\n🚀 Setting up authenticated session...');

        await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
        await page.waitForFunction(() => window.APP_STATE && window.authManager);

        // Quick auth
        const timestamp = Date.now();
        const testEmail = `errortest${timestamp}@example.com`;

        const registerTab = await page.$('[data-tab="register"]');
        if (registerTab) await registerTab.click();

        await page.fill('#register-firstname', 'ErrorTest');
        await page.fill('#register-lastname', 'User');
        await page.fill('#register-email', testEmail);
        await page.fill('#register-password', 'password123');
        await page.fill('#register-confirm-password', 'password123');

        const submitBtn = await page.$('#register-form button[type="submit"]');
        await submitBtn.click();
        await page.waitForTimeout(4000);

        // Check auth state
        const isAuthenticated = await page.evaluate(() => !!window.authManager?.isAuthenticated());
        if (!isAuthenticated) {
            console.log('❌ Authentication failed, cannot continue');
            return;
        }

        console.log('✅ Authentication successful, proceeding with error analysis...\n');

        // === SESSION CREATION ERROR ANALYSIS ===
        console.log('1️⃣ SESSION CREATION ANALYSIS');
        console.log('──────────────────────────────');

        // Navigate to sessions tab
        const sessionsTab = await page.$('[data-tab="sessions"]');
        if (sessionsTab) {
            console.log('📋 Navigating to Sessions tab...');
            await sessionsTab.click();
            await page.waitForTimeout(2000);
        }

        // Look for create session elements
        console.log('🔍 Searching for session creation elements...');

        const createSessionElements = await page.$$(`
            #create-session-btn,
            button:has-text("Create Session"),
            button:has-text("Add Session"),
            button:has-text("New Session"),
            [data-action="create-session"],
            .create-session-btn
        `);

        console.log(`   Found ${createSessionElements.length} potential session creation elements`);

        for (let i = 0; i < createSessionElements.length; i++) {
            const element = createSessionElements[i];
            try {
                const text = await element.textContent();
                const isVisible = await element.isVisible();
                const isEnabled = await element.isEnabled();
                const id = await element.getAttribute('id');
                const className = await element.getAttribute('class');

                console.log(`   Element ${i + 1}:`);
                console.log(`      Text: "${text}"`);
                console.log(`      Visible: ${isVisible}, Enabled: ${isEnabled}`);
                console.log(`      ID: ${id}, Class: ${className}`);

                if (isVisible && isEnabled) {
                    console.log(`   🎯 Attempting to click session creation button...`);

                    errors.length = 0; // Clear previous errors
                    await element.click();
                    await page.waitForTimeout(3000);

                    console.log(`   📊 Errors after click: ${errors.length}`);
                    if (errors.length > 0) {
                        console.log('   🔴 ERRORS DETECTED:');
                        errors.forEach(err => {
                            console.log(`      • ${err.type || 'API'}: ${err.message || err.status} ${err.endpoint || ''}`);
                        });
                    }

                    // Check if any modal/form appeared
                    const modal = await page.$('.modal, .popup, .dialog, [data-modal]');
                    const form = await page.$('#create-session-form, form[data-form="session"]');

                    if (modal) {
                        console.log('   ✅ Modal appeared for session creation');

                        // Try to find form fields
                        const nameField = await modal.$('input[name="sessionName"], input[name="name"], #session-name');
                        if (nameField) {
                            console.log('   📝 Found session name field, testing form submission...');

                            await nameField.fill(`Test Session ${timestamp}`);
                            await page.waitForTimeout(1000);

                            const submitBtn = await modal.$('button[type="submit"], button:has-text("Create"), button:has-text("Save")');
                            if (submitBtn) {
                                errors.length = 0; // Clear errors
                                console.log('   🚀 Submitting session creation form...');

                                await submitBtn.click();
                                await page.waitForTimeout(4000);

                                console.log(`   📊 Errors after submission: ${errors.length}`);
                                if (errors.length > 0) {
                                    console.log('   🔴 FORM SUBMISSION ERRORS:');
                                    errors.forEach(err => {
                                        console.log(`      • ${err.type || 'API'}: ${err.message || `${err.status} ${err.endpoint}`}`);
                                        if (err.body) {
                                            console.log(`        Body: ${err.body.substring(0, 200)}`);
                                        }
                                    });
                                }
                            } else {
                                console.log('   ❌ No submit button found in session form');
                            }
                        } else {
                            console.log('   ❌ No session name field found in modal');
                        }

                        // Close modal
                        const closeBtn = await modal.$('button:has-text("Close"), button:has-text("Cancel"), .close');
                        if (closeBtn) await closeBtn.click();

                    } else if (form) {
                        console.log('   ✅ Form appeared for session creation (no modal)');
                        // Handle direct form...
                    } else {
                        console.log('   ❌ No modal or form appeared after clicking');
                    }

                    break; // Only test the first working element
                }

            } catch (elementError) {
                console.log(`   ❌ Element ${i + 1} error: ${elementError.message}`);
            }
        }

        // === INVITE CREATION ERROR ANALYSIS ===
        console.log('\n2️⃣ INVITE CREATION ANALYSIS');
        console.log('─────────────────────────────');

        // Navigate to invites tab
        const invitesTab = await page.$('[data-tab="invites"]');
        if (invitesTab) {
            console.log('📨 Navigating to Invites tab...');
            await invitesTab.click();
            await page.waitForTimeout(2000);
        }

        const createInviteElements = await page.$$(`
            #generate-invite-btn,
            #create-invite-btn,
            button:has-text("Generate Invite"),
            button:has-text("Create Invite"),
            button:has-text("New Invite"),
            [data-action="generate-invite"]
        `);

        console.log(`🔍 Found ${createInviteElements.length} invite creation elements`);

        for (let element of createInviteElements.slice(0, 1)) { // Test first one only
            try {
                const isVisible = await element.isVisible();
                const isEnabled = await element.isEnabled();
                const text = await element.textContent();

                if (isVisible && isEnabled) {
                    console.log(`🎯 Testing invite creation: "${text}"`);

                    errors.length = 0;
                    await element.click();
                    await page.waitForTimeout(3000);

                    console.log(`📊 Errors after invite creation: ${errors.length}`);
                    if (errors.length > 0) {
                        console.log('🔴 INVITE CREATION ERRORS:');
                        errors.forEach(err => {
                            console.log(`   • ${err.type || 'API'}: ${err.message || `${err.status} ${err.endpoint}`}`);
                            if (err.body) {
                                console.log(`     Body: ${err.body.substring(0, 200)}`);
                            }
                        });
                    }
                    break;
                }
            } catch (inviteError) {
                console.log(`❌ Invite element error: ${inviteError.message}`);
            }
        }

        // === MODERATOR CREATION ERROR ANALYSIS ===
        console.log('\n3️⃣ MODERATOR CREATION ANALYSIS');
        console.log('────────────────────────────────');

        // Navigate to moderators/providers tab
        const moderatorsTab = await page.$('[data-tab="providers"], [data-tab="moderators"]');
        if (moderatorsTab) {
            console.log('🔧 Navigating to Moderators tab...');
            await moderatorsTab.click();
            await page.waitForTimeout(2000);
        }

        const createModeratorElements = await page.$$(`
            #create-provider-btn,
            button:has-text("Create Provider"),
            button:has-text("Add Provider"),
            button:has-text("New Provider"),
            button:has-text("Create Moderator"),
            [data-action="create-provider"]
        `);

        console.log(`🔍 Found ${createModeratorElements.length} moderator creation elements`);

        for (let element of createModeratorElements.slice(0, 1)) {
            try {
                const isVisible = await element.isVisible();
                const isEnabled = await element.isEnabled();
                const text = await element.textContent();

                if (isVisible && isEnabled) {
                    console.log(`🎯 Testing moderator creation: "${text}"`);

                    errors.length = 0;
                    await element.click();
                    await page.waitForTimeout(3000);

                    console.log(`📊 Errors after moderator creation: ${errors.length}`);
                    if (errors.length > 0) {
                        console.log('🔴 MODERATOR CREATION ERRORS:');
                        errors.forEach(err => {
                            console.log(`   • ${err.type || 'API'}: ${err.message || `${err.status} ${err.endpoint}`}`);
                            if (err.body) {
                                console.log(`     Body: ${err.body.substring(0, 200)}`);
                            }
                        });
                    }
                    break;
                }
            } catch (moderatorError) {
                console.log(`❌ Moderator element error: ${moderatorError.message}`);
            }
        }

        // === NETWORK ANALYSIS ===
        console.log('\n4️⃣ NETWORK REQUEST ANALYSIS');
        console.log('─────────────────────────────────');

        const failedRequests = networkRequests.filter(req =>
            req.url.includes('/api/v1.0/') &&
            (req.url.includes('session') || req.url.includes('invite') || req.url.includes('provider'))
        );

        console.log(`🌐 Found ${failedRequests.length} related network requests:`);

        failedRequests.forEach((req, i) => {
            console.log(`   ${i + 1}. ${req.method} ${req.url}`);
            if (req.postData) {
                console.log(`      Body: ${req.postData.substring(0, 100)}`);
            }
        });

        // === API ENDPOINT CHECK ===
        console.log('\n5️⃣ API ENDPOINT VERIFICATION');
        console.log('──────────────────────────────────');

        const currentEndpoint = await page.evaluate(() => window.APP_STATE?.apiEndpoint);
        console.log(`🎯 Current API Endpoint: ${currentEndpoint}`);

        if (currentEndpoint?.includes('api.wagl.ai')) {
            console.log('⚠️  SSL CERTIFICATE ISSUE DETECTED!');
            console.log('   The endpoint switched back to api.wagl.ai which has SSL issues');
            console.log('   This is likely the root cause of all creation failures');
        }

        console.log('\n' + '='.repeat(60));
        console.log('📋 DEEP DIVE ANALYSIS COMPLETE');
        console.log('='.repeat(60));

        // Final error summary
        const uniqueErrors = [...new Set(errors.map(e => e.message || `${e.status} ${e.endpoint}`))];
        console.log(`\n📊 SUMMARY:`);
        console.log(`   Total errors detected: ${errors.length}`);
        console.log(`   Unique error types: ${uniqueErrors.length}`);
        console.log(`   Network requests made: ${networkRequests.length}`);
        console.log(`   Current API endpoint: ${currentEndpoint}`);

        if (uniqueErrors.length > 0) {
            console.log('\n🔴 UNIQUE ERRORS:');
            uniqueErrors.forEach(error => {
                console.log(`   • ${error}`);
            });
        }

    } catch (testError) {
        console.error('\n💥 Test error:', testError.message);
        console.error(testError.stack);
    }

    console.log('\n⏸️  Press Enter to close and analyze results...');
    await new Promise(resolve => {
        process.stdin.once('data', resolve);
    });

    await browser.close();
}

deepDiveErrorsTest();