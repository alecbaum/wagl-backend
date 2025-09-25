// Wagl Backend API Demo - Main Application

// Main Application Class
class WaglDemoApp {
    constructor() {
        this.initialized = false;
        this.dashboardManager = null;
        this.chatManager = null;
        this.setupComplete = false;
    }

    // Initialize the application
    async initialize() {
        if (this.initialized) return;

        debugLog('Initializing Wagl Demo App');

        try {
            // Initialize core managers (already created in other files)
            // authManager, pageManager, tabManager, modalManager, signalRManager

            // Setup application event handlers
            this.setupEventHandlers();

            // Initialize dashboard and chat managers
            this.dashboardManager = new DashboardManager();
            this.publicDashboardManager = new PublicDashboardManager();
            this.chatManager = new ChatManager();

            // Export to global scope for access
            window.publicDashboardManager = this.publicDashboardManager;

            // Setup form handlers
            this.setupFormHandlers();

            // Initialize based on authentication state
            this.initializeBasedOnAuth();

            this.initialized = true;
            debugLog('Wagl Demo App initialized successfully');

        } catch (error) {
            errorLog(error, 'WaglDemoApp.initialize');
            ToastManager.showError('Initialization Error', 'Failed to initialize the application');
        }
    }

    // Setup global event handlers
    setupEventHandlers() {
        // API endpoint toggle
        const toggleBtn = DOMUtils.$('#toggle-endpoint');
        if (toggleBtn) {
            DOMUtils.on(toggleBtn, 'click', () => {
                const newEndpoint = window.apiClient.switchEndpoint();
                const endpointDisplay = DOMUtils.$('#current-endpoint');
                if (endpointDisplay) {
                    endpointDisplay.textContent = newEndpoint;
                }

                const isDirect = newEndpoint === CONFIG.API.DIRECT_BASE_URL;
                toggleBtn.textContent = isDirect ? 'Switch to Custom Domain' : 'Switch to Direct URL';

                ToastManager.showInfo('Endpoint Changed', `Now using: ${newEndpoint}`);
            });
        }

        // Logout button
        const logoutBtn = DOMUtils.$('#logout-btn');
        if (logoutBtn) {
            DOMUtils.on(logoutBtn, 'click', async () => {
                await window.authManager.logout();
            });
        }

        // Modal trigger buttons
        const createSessionBtn = DOMUtils.$('#create-session-btn');
        if (createSessionBtn) {
            DOMUtils.on(createSessionBtn, 'click', () => {
                this.openCreateSessionModal();
            });
        }

        const generateInviteBtn = DOMUtils.$('#generate-invite-btn');
        if (generateInviteBtn) {
            DOMUtils.on(generateInviteBtn, 'click', () => {
                this.openGenerateInviteModal();
            });
        }

        // Anonymous invite validation
        const validateInviteBtn = DOMUtils.$('#validate-invite');
        if (validateInviteBtn) {
            DOMUtils.on(validateInviteBtn, 'click', async () => {
                await this.validateInvite();
            });
        }

        const joinAnonymousBtn = DOMUtils.$('#join-anonymous');
        if (joinAnonymousBtn) {
            DOMUtils.on(joinAnonymousBtn, 'click', async () => {
                await this.joinAnonymous();
            });
        }

        // Public dashboard navigation
        const publicDashboardBtn = DOMUtils.$('#public-dashboard-nav');
        if (publicDashboardBtn) {
            DOMUtils.on(publicDashboardBtn, 'click', () => {
                window.pageManager.showPage('public-dashboard');
            });
        }

        // Moderator authentication
        const authenticateModeratorBtn = DOMUtils.$('#authenticate-moderator');
        if (authenticateModeratorBtn) {
            DOMUtils.on(authenticateModeratorBtn, 'click', async () => {
                await this.authenticateModerator();
            });
        }

        const joinAsModeratorBtn = DOMUtils.$('#join-as-moderator');
        if (joinAsModeratorBtn) {
            DOMUtils.on(joinAsModeratorBtn, 'click', async () => {
                await this.joinAsModerator();
            });
        }

        const disconnectModeratorBtn = DOMUtils.$('#disconnect-moderator');
        if (disconnectModeratorBtn) {
            DOMUtils.on(disconnectModeratorBtn, 'click', () => {
                this.disconnectModerator();
            });
        }

        // Create provider button
        const createProviderBtn = DOMUtils.$('#create-provider-btn');
        if (createProviderBtn) {
            DOMUtils.on(createProviderBtn, 'click', () => {
                this.openCreateProviderModal();
            });
        }

        // API testing button
        const testAllEndpointsBtn = DOMUtils.$('#test-all-endpoints');
        if (testAllEndpointsBtn) {
            DOMUtils.on(testAllEndpointsBtn, 'click', async () => {
                await this.testAllEndpoints();
            });
        }

        debugLog('Event handlers setup completed');
    }

    // Setup form handlers
    setupFormHandlers() {
        // Login form - NO VALIDATION
        FormManager.setupForm('#login-form', async (data) => {
            await window.authManager.login(data.email || '', data.password || '', data.rememberMe || false);
        });

        // Register form - NO VALIDATION
        FormManager.setupForm('#register-form', async (data) => {
            const password = data.password || 'password123';
            const userData = {
                firstName: data.firstName || 'Test',
                lastName: data.lastName || 'User',
                email: data.email || 'test@example.com',
                password: password,
                confirmPassword: data.confirmPassword || password, // Use same as password if not provided
                requestedTier: parseInt(data.tier?.replace('Tier', '') || '1') // Convert "Tier1" to 1
            };

            await window.authManager.register(userData);
        });

        // Create session form - NO VALIDATION
        FormManager.setupForm('#create-session-form', async (data) => {
            const sessionData = {
                name: data.sessionName || 'Test Session',
                scheduledStartTime: data.startTime || new Date().toISOString(),
                durationMinutes: parseInt(data.duration) || 60,
                maxParticipants: parseInt(data.maxParticipants) || 18,
                maxParticipantsPerRoom: parseInt(data.maxPerRoom) || 6
            };

            const response = await window.apiClient.createSession(sessionData);
            ToastManager.showSuccess('Session Created', `Session "${response.name}" has been created successfully`);

            window.modalManager.closeModal();
            if (this.dashboardManager) {
                this.dashboardManager.refreshSessions();
            }
        });

        // Generate invite form - NO VALIDATION
        FormManager.setupForm('#generate-invite-form', async (data) => {
            const response = await window.apiClient.generateInvite(data.sessionId || '', parseInt(data.expiration) || 120);
            ToastManager.showSuccess('Invite Generated', 'Invite code has been generated successfully');

            window.modalManager.closeModal();
            if (this.dashboardManager) {
                this.dashboardManager.refreshInvites();
            }
        });

        debugLog('Form handlers setup completed');
    }

    // Initialize based on current authentication state
    initializeBasedOnAuth() {
        if (window.authManager.isAuthenticated()) {
            const user = window.authManager.getCurrentUser();
            if (user && user.isAnonymous) {
                window.pageManager.showPage('anonymous-chat');
            } else {
                window.pageManager.showPage('dashboard');
                // Connect SignalR for authenticated users
                if (CONFIG.FEATURES.REALTIME_FEATURES) {
                    window.signalRManager.connect();
                }
            }
        } else {
            // Start with authentication page for easier registration/login
            window.pageManager.showPage('auth');
        }

        debugLog('Application initialized based on auth state');
    }

    // Open create session modal
    async openCreateSessionModal() {
        // Set default start time to now + 1 hour
        const defaultStartTime = new Date();
        defaultStartTime.setHours(defaultStartTime.getHours() + 1);

        const startTimeInput = DOMUtils.$('#session-start-time');
        if (startTimeInput) {
            startTimeInput.value = DateUtils.formatForInput(defaultStartTime.toISOString());
        }

        window.modalManager.showModal('create-session-modal');
    }

    // Open generate invite modal
    async openGenerateInviteModal() {
        try {
            // Load sessions for the dropdown
            const sessionsResponse = await window.apiClient.getSessions(1, 50);
            const sessionSelect = DOMUtils.$('#invite-session-select');

            if (sessionSelect && sessionsResponse.items) {
                sessionSelect.innerHTML = '<option value="">Select a session...</option>';

                sessionsResponse.items
                    .filter(session => session.status === 'Scheduled' || session.status === 'Active')
                    .forEach(session => {
                        const option = DOMUtils.create('option', { value: session.id },
                            `${session.name} (${session.status})`);
                        sessionSelect.appendChild(option);
                    });
            }

            window.modalManager.showModal('generate-invite-modal');

        } catch (error) {
            errorLog(error, 'openGenerateInviteModal');
            ToastManager.showError('Load Error', 'Failed to load sessions');
        }
    }

    // Validate invite code
    async validateInvite() {
        const inviteCodeInput = DOMUtils.$('#anonymous-invite-code');
        const inviteCode = inviteCodeInput?.value?.trim();

        if (!inviteCode) {
            ToastManager.showWarning('Validation Error', 'Please enter an invite code');
            return;
        }

        try {
            const response = await window.apiClient.validateInvite(inviteCode);

            // Show invite details
            const inviteDetails = DOMUtils.$('#invite-details');
            const sessionNameEl = DOMUtils.$('#session-name');
            const currentParticipantsEl = DOMUtils.$('#current-participants');
            const maxParticipantsEl = DOMUtils.$('#max-participants');

            if (sessionNameEl) sessionNameEl.textContent = response.sessionName;
            if (currentParticipantsEl) currentParticipantsEl.textContent = response.currentParticipants;
            if (maxParticipantsEl) maxParticipantsEl.textContent = response.maxParticipants;

            DOMUtils.show(inviteDetails);

            ToastManager.showSuccess('Valid Invite', 'Invite code is valid and ready to use');

        } catch (error) {
            ToastManager.showError('Invalid Invite', getFriendlyErrorMessage(error));
            DOMUtils.hide('#invite-details');
        }
    }

    // Join session anonymously
    async joinAnonymous() {
        const inviteCode = DOMUtils.$('#anonymous-invite-code')?.value?.trim();
        const displayName = DOMUtils.$('#anonymous-display-name')?.value?.trim();
        const email = DOMUtils.$('#anonymous-email')?.value?.trim() || null;

        if (!inviteCode || !displayName) {
            ToastManager.showWarning('Validation Error', 'Please fill in all required fields');
            return;
        }

        if (!ValidationUtils.isValidDisplayName(displayName)) {
            ToastManager.showWarning('Validation Error', 'Please enter a valid display name');
            return;
        }

        if (email && !ValidationUtils.isValidEmail(email)) {
            ToastManager.showWarning('Validation Error', 'Please enter a valid email address');
            return;
        }

        try {
            await window.authManager.joinAnonymous(inviteCode, displayName, email);

            // Connect SignalR for anonymous users
            if (CONFIG.FEATURES.REALTIME_FEATURES) {
                setTimeout(() => {
                    window.signalRManager.connect();
                }, 1000);
            }

        } catch (error) {
            // Error handling is done in authManager
        }
    }

    // Authenticate moderator with API key
    async authenticateModerator() {
        const apiKeyInput = DOMUtils.$('#moderator-api-key');
        const apiKey = apiKeyInput?.value?.trim();

        if (!apiKey) {
            ToastManager.showWarning('Authentication Error', 'Please enter an API key');
            return;
        }

        try {
            const response = await window.apiClient.authenticateWithApiKey(apiKey);

            // Show moderator info
            const moderatorInfo = DOMUtils.$('#moderator-info');
            const providerNameEl = DOMUtils.$('#moderator-provider-name');

            if (providerNameEl) providerNameEl.textContent = response.organizationName || 'Moderator';
            DOMUtils.show(moderatorInfo);

            // Load available rooms
            await this.loadModeratorRooms();

            ToastManager.showSuccess('Authenticated', `Welcome ${response.organizationName || 'Moderator'}`);

        } catch (error) {
            ToastManager.showError('Authentication Failed', getFriendlyErrorMessage(error));
        }
    }

    // Load rooms for moderator
    async loadModeratorRooms() {
        try {
            const response = await window.apiClient.moderatorGetRooms();
            const roomSelect = DOMUtils.$('#moderator-room-select');

            if (roomSelect && response.items) {
                roomSelect.innerHTML = '<option value="">Select a room...</option>';

                response.items.forEach(room => {
                    const option = DOMUtils.create('option', { value: room.id },
                        `${room.name} (${room.sessionName}) - ${room.currentParticipants} participants`);
                    roomSelect.appendChild(option);
                });
            }
        } catch (error) {
            errorLog(error, 'loadModeratorRooms');
        }
    }

    // Join room as moderator
    async joinAsModerator() {
        const roomSelect = DOMUtils.$('#moderator-room-select');
        const roomId = roomSelect?.value;

        if (!roomId) {
            ToastManager.showWarning('Selection Error', 'Please select a room to moderate');
            return;
        }

        try {
            const provider = window.APP_STATE.moderator.provider;
            await window.apiClient.moderatorJoinRoom(roomId, `Moderator (${provider?.organizationName || 'System'})`);

            // Update UI and switch to moderator chat page
            const selectedOption = roomSelect.options[roomSelect.selectedIndex];
            const roomName = selectedOption.text.split(' (')[0];

            const providerDisplayEl = DOMUtils.$('#moderator-provider-display');
            const moderatorRoomNameEl = DOMUtils.$('#moderator-room-name');

            if (providerDisplayEl) providerDisplayEl.textContent = provider?.organizationName || 'System';
            if (moderatorRoomNameEl) moderatorRoomNameEl.textContent = roomName;

            // Set current room state
            window.APP_STATE.chat.currentRoomId = roomId;

            // Navigate to moderator chat
            window.pageManager.showPage('moderator-chat');

            // Connect SignalR and join room
            if (CONFIG.FEATURES.REALTIME_FEATURES) {
                await window.signalRManager.connect();
                await window.signalRManager.joinRoom(roomId, `Moderator (${provider?.organizationName || 'System'})`);
            }

            ToastManager.showSuccess('Joined', `Connected to ${roomName} as moderator`);

        } catch (error) {
            ToastManager.showError('Join Failed', getFriendlyErrorMessage(error));
        }
    }

    // Disconnect moderator
    disconnectModerator() {
        // Clear moderator state
        window.APP_STATE.moderator.apiKey = null;
        window.APP_STATE.moderator.isActive = false;
        window.APP_STATE.moderator.provider = null;

        // Disconnect SignalR
        if (window.signalRManager) {
            window.signalRManager.disconnect();
        }

        // Navigate back to auth
        window.pageManager.showPage('auth');

        ToastManager.showInfo('Disconnected', 'Moderator session ended');
    }

    // Open create provider modal
    openCreateProviderModal() {
        window.modalManager.showModal('create-provider-modal');
    }

    // Test all API endpoints
    async testAllEndpoints() {
        const resultsContainer = DOMUtils.$('#api-test-results');
        if (!resultsContainer) return;

        resultsContainer.innerHTML = '<div class="loading">Testing API endpoints...</div>';

        const tests = [
            { name: 'Health Check', test: () => window.apiClient.healthCheck() },
            { name: 'Get Sessions (requires auth)', test: () => window.apiClient.getSessions(1, 10) },
            { name: 'Get Rooms (requires auth)', test: () => window.apiClient.getRooms(null, 1, 10) },
            { name: 'Get Invites (requires auth)', test: () => window.apiClient.getInvites(null, 1, 10) },
            {
                name: 'API Base Path Test',
                test: async () => {
                    const response = await fetch(`${window.APP_STATE.apiEndpoint}/api`);
                    return { status: response.status, statusText: response.statusText };
                }
            },
            {
                name: 'Root Path Test',
                test: async () => {
                    const response = await fetch(`${window.APP_STATE.apiEndpoint}/`);
                    return { status: response.status, statusText: response.statusText };
                }
            }
        ];

        const results = [];

        for (const testCase of tests) {
            try {
                const result = await testCase.test();
                results.push({
                    name: testCase.name,
                    status: 'success',
                    result: result
                });
            } catch (error) {
                results.push({
                    name: testCase.name,
                    status: 'error',
                    error: error.message || error
                });
            }
        }

        // Display results
        DataListManager.renderList('api-test-results', results, (result) => {
            const statusClass = result.status === 'success' ? 'badge-success' : 'badge-error';

            let content;
            if (result.status === 'success') {
                const resultData = typeof result.result === 'object' ? JSON.stringify(result.result, null, 2) : result.result;
                content = `
                    <strong>Status:</strong> <span class="badge ${statusClass}">SUCCESS</span><br>
                    <strong>Response:</strong><br>
                    <pre style="background: #f5f5f5; padding: 10px; border-radius: 4px; font-size: 12px; max-height: 100px; overflow-y: auto;">${resultData}</pre>
                `;
            } else {
                content = `
                    <strong>Status:</strong> <span class="badge ${statusClass}">ERROR</span><br>
                    <strong>Error:</strong> ${result.error}
                `;
            }

            return DataListManager.createCard(result.name, content);
        });
    }
}

// Dashboard Manager
class DashboardManager {
    constructor() {
        this.refreshInterval = null;
        this.stats = {
            totalSessions: 0,
            activeSessions: 0,
            totalParticipants: 0
        };

        this.setupDashboard();
    }

    setupDashboard() {
        // Auto-refresh dashboard data
        if (CONFIG.UI.DASHBOARD_REFRESH_INTERVAL > 0) {
            this.refreshInterval = setInterval(() => {
                if (window.pageManager.getCurrentPage() === 'dashboard') {
                    this.refreshData();
                }
            }, CONFIG.UI.DASHBOARD_REFRESH_INTERVAL);
        }

        // Setup session filter for rooms tab
        const sessionFilter = DOMUtils.$('#session-filter');
        if (sessionFilter) {
            DOMUtils.on(sessionFilter, 'change', () => {
                this.refreshRooms();
            });
        }

        debugLog('Dashboard manager setup completed');
    }

    async refreshData() {
        await Promise.all([
            this.refreshStats(),
            this.refreshSessions(),
            this.refreshRooms(),
            this.refreshInvites()
        ]);
    }

    async refreshStats() {
        try {
            const stats = await window.apiClient.getDashboardStats();

            this.stats = stats;

            // Update UI
            const totalSessionsEl = DOMUtils.$('#total-sessions');
            const activeSessionsEl = DOMUtils.$('#active-sessions');
            const totalParticipantsEl = DOMUtils.$('#total-participants');

            if (totalSessionsEl) totalSessionsEl.textContent = stats.totalSessions || 0;
            if (activeSessionsEl) activeSessionsEl.textContent = stats.activeSessions || 0;
            if (totalParticipantsEl) totalParticipantsEl.textContent = stats.totalParticipants || 0;

        } catch (error) {
            debugLog('Dashboard stats not available - likely missing endpoint');
        }
    }

    async refreshSessions() {
        try {
            const response = await window.apiClient.getSessions(1, 50);

            DataListManager.renderList('sessions-list', response.items, (session) => {
                return this.renderSessionCard(session);
            });

        } catch (error) {
            errorLog(error, 'DashboardManager.refreshSessions');
        }
    }

    async refreshRooms() {
        try {
            const sessionFilter = DOMUtils.$('#session-filter');
            const selectedSessionId = sessionFilter?.value || null;

            const response = await window.apiClient.getRooms(selectedSessionId, 1, 50);

            DataListManager.renderList('rooms-list', response.items, (room) => {
                return this.renderRoomCard(room);
            });

        } catch (error) {
            errorLog(error, 'DashboardManager.refreshRooms');
        }
    }

    async refreshInvites() {
        try {
            const response = await window.apiClient.getInvites(null, 1, 50);

            DataListManager.renderList('invites-list', response.items, (invite) => {
                return this.renderInviteCard(invite);
            });

        } catch (error) {
            errorLog(error, 'DashboardManager.refreshInvites');
        }
    }

    renderSessionCard(session) {
        const statusClass = session.status === 'Active' ? 'badge-success' :
                          session.status === 'Scheduled' ? 'badge-info' : 'badge-secondary';

        const actions = [
            {
                text: 'View Rooms',
                class: 'btn-outline',
                onClick: () => this.viewSessionRooms(session.id)
            }
        ];

        if (session.status === 'Scheduled') {
            actions.push({
                text: 'Start',
                class: 'btn-primary',
                onClick: () => this.startSession(session.id)
            });
        }

        if (session.status === 'Active') {
            actions.push({
                text: 'End',
                class: 'btn-warning',
                onClick: () => this.endSession(session.id)
            });
        }

        const content = `
            <div class="data-grid cols-2">
                <div>
                    <strong>Status:</strong>
                    <span class="badge ${statusClass}">${session.status}</span>
                </div>
                <div><strong>Participants:</strong> ${session.currentParticipants}/${session.maxParticipants}</div>
                <div><strong>Start Time:</strong> ${DateUtils.formatDate(session.scheduledStartTime)}</div>
                <div><strong>Duration:</strong> ${session.durationMinutes} minutes</div>
            </div>
        `;

        return DataListManager.createCard(session.name, content, actions);
    }

    renderRoomCard(room) {
        const actions = [
            {
                text: 'Join Room',
                class: 'btn-primary',
                onClick: () => this.joinRoom(room.id, room.name)
            }
        ];

        const content = `
            <div class="data-grid cols-2">
                <div><strong>Session:</strong> ${room.sessionName}</div>
                <div><strong>Participants:</strong> ${room.currentParticipants}/${room.maxParticipants}</div>
                <div><strong>Status:</strong> <span class="badge badge-info">${room.isActive ? 'Active' : 'Inactive'}</span></div>
                <div><strong>Created:</strong> ${DateUtils.formatDate(room.createdAt)}</div>
            </div>
        `;

        return DataListManager.createCard(room.name, content, actions);
    }

    renderInviteCard(invite) {
        const isExpired = DateUtils.isPast(invite.expiresAt);
        const statusClass = isExpired ? 'badge-error' : 'badge-success';

        const actions = [
            {
                text: 'Copy Code',
                class: 'btn-outline',
                onClick: () => this.copyInviteCode(invite.code)
            }
        ];

        if (!isExpired) {
            actions.push({
                text: 'Revoke',
                class: 'btn-warning',
                onClick: () => this.revokeInvite(invite.id)
            });
        }

        const content = `
            <div class="data-grid cols-2">
                <div><strong>Session:</strong> ${invite.sessionName}</div>
                <div><strong>Status:</strong> <span class="badge ${statusClass}">${isExpired ? 'Expired' : 'Active'}</span></div>
                <div><strong>Expires:</strong> ${DateUtils.formatDate(invite.expiresAt)}</div>
                <div><strong>Used:</strong> ${invite.usedCount}/${invite.maxUses || '∞'} times</div>
            </div>
            <div style="margin-top: 10px;">
                <div class="copy-container">
                    <input class="copy-input" readonly value="${invite.code}">
                    <button class="copy-btn">📋</button>
                </div>
            </div>
        `;

        const card = DataListManager.createCard(`Invite Code`, content, actions);

        // Setup copy functionality
        const copyBtn = card.querySelector('.copy-btn');
        const copyInput = card.querySelector('.copy-input');

        if (copyBtn && copyInput) {
            DOMUtils.on(copyBtn, 'click', async () => {
                const success = await HttpUtils.copyToClipboard(invite.code);
                if (success) {
                    ToastManager.showSuccess('Copied!', 'Invite code copied to clipboard');
                }
            });
        }

        return card;
    }

    async startSession(sessionId) {
        try {
            await window.apiClient.startSession(sessionId);
            ToastManager.showSuccess('Session Started', 'Session has been started successfully');
            this.refreshSessions();
        } catch (error) {
            ToastManager.showError('Start Failed', getFriendlyErrorMessage(error));
        }
    }

    async endSession(sessionId) {
        if (!confirm('Are you sure you want to end this session?')) return;

        try {
            await window.apiClient.endSession(sessionId);
            ToastManager.showSuccess('Session Ended', 'Session has been ended successfully');
            this.refreshSessions();
        } catch (error) {
            ToastManager.showError('End Failed', getFriendlyErrorMessage(error));
        }
    }

    async joinRoom(roomId, roomName) {
        try {
            const user = window.authManager.getCurrentUser();
            const displayName = user?.firstName || user?.displayName || 'Admin';

            await window.apiClient.joinRoom(roomId, displayName);

            // Switch to live chat tab and connect to room
            window.tabManager.showTab(DOMUtils.$('[data-tab="live-chat"]'), 'live-chat');

            if (this.chatManager) {
                this.chatManager.joinRoom(roomId, roomName);
            }

        } catch (error) {
            ToastManager.showError('Join Failed', getFriendlyErrorMessage(error));
        }
    }

    async copyInviteCode(code) {
        const success = await HttpUtils.copyToClipboard(code);
        if (success) {
            ToastManager.showSuccess('Copied!', 'Invite code copied to clipboard');
        }
    }

    async revokeInvite(inviteId) {
        if (!confirm('Are you sure you want to revoke this invite?')) return;

        try {
            await window.apiClient.revokeInvite(inviteId);
            ToastManager.showSuccess('Invite Revoked', 'Invite has been revoked successfully');
            this.refreshInvites();
        } catch (error) {
            ToastManager.showError('Revoke Failed', getFriendlyErrorMessage(error));
        }
    }

    viewSessionRooms(sessionId) {
        const sessionFilter = DOMUtils.$('#session-filter');
        if (sessionFilter) {
            // Update filter to show only this session's rooms
            sessionFilter.value = sessionId;
            this.refreshRooms();

            // Switch to rooms tab
            window.tabManager.showTab(DOMUtils.$('[data-tab="rooms"]'), 'rooms');
        }
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
}

// Chat Manager
class ChatManager {
    constructor() {
        this.currentRoomId = null;
        this.currentRoomName = '';
        this.messageContainer = null;
        this.participantsContainer = null;
        this.typingTimeout = null;

        this.setupChatInterface();
        this.setupSignalRHandlers();
    }

    setupChatInterface() {
        // Live chat interface
        const sendBtn = DOMUtils.$('#send-message-btn');
        const messageInput = DOMUtils.$('#message-input');
        const leaveRoomBtn = DOMUtils.$('#leave-room-btn');

        if (sendBtn) {
            DOMUtils.on(sendBtn, 'click', () => this.sendMessage());
        }

        if (messageInput) {
            DOMUtils.on(messageInput, 'keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendMessage();
                }
            });

            // Typing indicators
            DOMUtils.on(messageInput, 'input', debounce(() => {
                if (this.currentRoomId && window.signalRManager) {
                    window.signalRManager.sendTyping(this.currentRoomId, true);

                    clearTimeout(this.typingTimeout);
                    this.typingTimeout = setTimeout(() => {
                        window.signalRManager.sendTyping(this.currentRoomId, false);
                    }, 2000);
                }
            }, 300));
        }

        if (leaveRoomBtn) {
            DOMUtils.on(leaveRoomBtn, 'click', () => this.leaveCurrentRoom());
        }

        // Anonymous chat interface
        const anonSendBtn = DOMUtils.$('#anonymous-send-message-btn');
        const anonMessageInput = DOMUtils.$('#anonymous-message-input');

        if (anonSendBtn) {
            DOMUtils.on(anonSendBtn, 'click', () => this.sendAnonymousMessage());
        }

        if (anonMessageInput) {
            DOMUtils.on(anonMessageInput, 'keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendAnonymousMessage();
                }
            });
        }

        debugLog('Chat interface setup completed');
    }

    setupSignalRHandlers() {
        if (!window.signalRManager) return;

        // Handle incoming messages
        window.signalRManager.onMessage('message', (message) => {
            this.addMessage(message);
        });

        // Handle user events
        window.signalRManager.onMessage('user-joined', (participant) => {
            this.addSystemMessage(`${participant.displayName} joined the room`);
            this.updateParticipantsList();
        });

        window.signalRManager.onMessage('user-left', (participant) => {
            this.addSystemMessage(`${participant.displayName} left the room`);
            this.updateParticipantsList();
        });

        // Handle participants updates
        window.signalRManager.onMessage('participants-updated', (participants) => {
            this.updateParticipantsDisplay(participants);
        });

        debugLog('SignalR handlers setup completed');
    }

    async initializeAnonymousChat() {
        const session = window.APP_STATE.ui.selectedSession;
        const room = window.APP_STATE.ui.selectedRoom;
        const user = window.authManager.getCurrentUser();

        if (session && room && user) {
            // Update UI with session/room info
            const sessionNameEl = DOMUtils.$('#anonymous-session-name');
            const userNameEl = DOMUtils.$('#anonymous-user-name');
            const roomNameEl = DOMUtils.$('#anonymous-room-name');

            if (sessionNameEl) sessionNameEl.textContent = session.name;
            if (userNameEl) userNameEl.textContent = user.displayName;
            if (roomNameEl) roomNameEl.textContent = room.name;

            // Join the room via SignalR
            if (window.signalRManager && window.signalRManager.isConnected()) {
                try {
                    await window.signalRManager.joinRoom(room.id, user.displayName);
                    this.currentRoomId = room.id;
                    this.currentRoomName = room.name;

                    // Load message history
                    await this.loadMessages(room.id);

                } catch (error) {
                    errorLog(error, 'ChatManager.initializeAnonymousChat');
                }
            }
        }
    }

    async initializeModeratorChat() {
        const roomId = window.APP_STATE.chat.currentRoomId;
        if (roomId && window.signalRManager && window.signalRManager.isConnected()) {
            // Load message history for moderator
            await this.loadMessages(roomId, '#moderator-messages-container');
        }
    }

    async joinRoom(roomId, roomName) {
        try {
            if (this.currentRoomId && this.currentRoomId !== roomId) {
                await this.leaveCurrentRoom();
            }

            if (window.signalRManager && window.signalRManager.isConnected()) {
                const user = window.authManager.getCurrentUser();
                const displayName = user?.firstName || user?.displayName || 'Admin';

                await window.signalRManager.joinRoom(roomId, displayName);
            }

            this.currentRoomId = roomId;
            this.currentRoomName = roomName;

            // Update UI
            const roomNameEl = DOMUtils.$('#current-room-name');
            const messageInput = DOMUtils.$('#message-input');
            const sendBtn = DOMUtils.$('#send-message-btn');
            const leaveBtn = DOMUtils.$('#leave-room-btn');

            if (roomNameEl) roomNameEl.textContent = roomName;
            if (messageInput) messageInput.disabled = false;
            if (sendBtn) sendBtn.disabled = false;
            if (leaveBtn) leaveBtn.disabled = false;

            // Load message history
            await this.loadMessages(roomId);

            ToastManager.showSuccess('Joined Room', `Connected to ${roomName}`);

        } catch (error) {
            errorLog(error, 'ChatManager.joinRoom');
            ToastManager.showError('Join Failed', getFriendlyErrorMessage(error));
        }
    }

    async leaveCurrentRoom() {
        if (!this.currentRoomId) return;

        try {
            if (window.signalRManager) {
                await window.signalRManager.leaveRoom(this.currentRoomId);
            }

            // Update UI
            const roomNameEl = DOMUtils.$('#current-room-name');
            const messageInput = DOMUtils.$('#message-input');
            const sendBtn = DOMUtils.$('#send-message-btn');
            const leaveBtn = DOMUtils.$('#leave-room-btn');

            if (roomNameEl) roomNameEl.textContent = 'Select a room to join';
            if (messageInput) {
                messageInput.disabled = true;
                messageInput.value = '';
            }
            if (sendBtn) sendBtn.disabled = true;
            if (leaveBtn) leaveBtn.disabled = true;

            // Clear messages
            const messagesContainer = DOMUtils.$('#messages-container');
            if (messagesContainer) messagesContainer.innerHTML = '';

            ToastManager.showInfo('Left Room', `Disconnected from ${this.currentRoomName}`);

            this.currentRoomId = null;
            this.currentRoomName = '';

        } catch (error) {
            errorLog(error, 'ChatManager.leaveCurrentRoom');
        }
    }

    async loadMessages(roomId, containerSelector = null) {
        try {
            const response = await window.apiClient.getRoomMessages(roomId, 1, 50);

            const messagesContainer = containerSelector ?
                DOMUtils.$(containerSelector) :
                (DOMUtils.$('#messages-container') || DOMUtils.$('#anonymous-messages-container'));

            if (messagesContainer) {
                messagesContainer.innerHTML = '';

                if (response.items && response.items.length > 0) {
                    response.items.reverse().forEach(message => {
                        this.addMessage(message, false);
                    });
                } else {
                    this.addSystemMessage('No messages yet. Start the conversation!', false);
                }

                // Scroll to bottom
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }

        } catch (error) {
            errorLog(error, 'ChatManager.loadMessages');
        }
    }

    async sendMessage() {
        const messageInput = DOMUtils.$('#message-input');
        const content = messageInput?.value?.trim();

        if (!content || !this.currentRoomId) return;

        if (!ValidationUtils.isValidMessage(content)) {
            ToastManager.showWarning('Message Error', 'Message is too long or empty');
            return;
        }

        try {
            if (window.signalRManager && window.signalRManager.isConnected()) {
                await window.signalRManager.sendMessage(this.currentRoomId, content);
            } else {
                // Fallback to API call
                await window.apiClient.sendMessage(this.currentRoomId, content);
            }

            messageInput.value = '';

        } catch (error) {
            errorLog(error, 'ChatManager.sendMessage');
            ToastManager.showError('Send Failed', getFriendlyErrorMessage(error));
        }
    }

    async sendAnonymousMessage() {
        const messageInput = DOMUtils.$('#anonymous-message-input');
        const content = messageInput?.value?.trim();
        const room = window.APP_STATE.ui.selectedRoom;

        if (!content || !room) return;

        if (!ValidationUtils.isValidMessage(content)) {
            ToastManager.showWarning('Message Error', 'Message is too long or empty');
            return;
        }

        try {
            if (window.signalRManager && window.signalRManager.isConnected()) {
                await window.signalRManager.sendMessage(room.id, content);
            }

            messageInput.value = '';

        } catch (error) {
            errorLog(error, 'ChatManager.sendAnonymousMessage');
            ToastManager.showError('Send Failed', getFriendlyErrorMessage(error));
        }
    }

    addMessage(message, animate = true) {
        const messagesContainer = DOMUtils.$('#messages-container') || DOMUtils.$('#anonymous-messages-container');
        if (!messagesContainer) return;

        const messageEl = DOMUtils.create('div', { class: 'message' });

        const header = DOMUtils.create('div', { class: 'message-header' });
        const senderName = DOMUtils.create('span', { class: 'message-sender' }, message.senderName);
        const timestamp = DOMUtils.create('span', { class: 'message-time' }, DateUtils.formatDate(message.sentAt));

        header.appendChild(senderName);
        header.appendChild(timestamp);

        const content = DOMUtils.create('div', { class: 'message-content' }, message.content);

        messageEl.appendChild(header);
        messageEl.appendChild(content);

        messagesContainer.appendChild(messageEl);

        // Animate if requested
        if (animate) {
            AnimationUtils.fadeIn(messageEl);
        }

        // Scroll to bottom
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Limit message history
        const messages = messagesContainer.querySelectorAll('.message');
        if (messages.length > CONFIG.UI.MAX_MESSAGES_DISPLAY) {
            messagesContainer.removeChild(messages[0]);
        }
    }

    addSystemMessage(content, animate = true) {
        const messagesContainer = DOMUtils.$('#messages-container') || DOMUtils.$('#anonymous-messages-container');
        if (!messagesContainer) return;

        const messageEl = DOMUtils.create('div', { class: 'message system-message' });
        const contentEl = DOMUtils.create('div', { class: 'message-content' }, content);

        messageEl.appendChild(contentEl);
        messagesContainer.appendChild(messageEl);

        if (animate) {
            AnimationUtils.fadeIn(messageEl);
        }

        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    async updateParticipantsList() {
        if (!this.currentRoomId) return;

        try {
            const participants = await window.apiClient.getRoomParticipants(this.currentRoomId);
            this.updateParticipantsDisplay(participants);
        } catch (error) {
            debugLog('Failed to update participants list:', error);
        }
    }

    updateParticipantsDisplay(participants) {
        const participantsContainer = DOMUtils.$('#room-participants') || DOMUtils.$('#anonymous-room-participants');
        if (!participantsContainer || !participants) return;

        participantsContainer.innerHTML = '';

        if (participants.length === 0) {
            participantsContainer.innerHTML = '<div class="empty-state">No participants</div>';
            return;
        }

        participants.forEach(participant => {
            const participantEl = DOMUtils.create('div', { class: 'participant' });
            participantEl.innerHTML = `
                <div class="participant-info">
                    <span class="participant-name">${participant.displayName}</span>
                    <span class="participant-status ${participant.isOnline ? 'online' : 'offline'}"></span>
                </div>
            `;
            participantsContainer.appendChild(participantEl);
        });
    }

    destroy() {
        if (this.typingTimeout) {
            clearTimeout(this.typingTimeout);
        }
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    debugLog('DOM Content Loaded - Initializing Wagl Demo App');

    const app = new WaglDemoApp();
    window.waglApp = app;

    app.initialize().then(() => {
        debugLog('Wagl Demo App ready!');
    }).catch(error => {
        errorLog(error, 'App initialization failed');
        ToastManager.showError('Initialization Error', 'Failed to start the application');
    });
});

// Public Dashboard Manager (No authentication required)
class PublicDashboardManager {
    constructor() {
        this.refreshInterval = null;
        this.setupPublicDashboard();
    }

    setupPublicDashboard() {
        // Auto-refresh public dashboard data
        if (CONFIG.UI.DASHBOARD_REFRESH_INTERVAL > 0) {
            this.refreshInterval = setInterval(() => {
                if (window.pageManager.getCurrentPage() === 'public-dashboard') {
                    this.refreshData();
                }
            }, CONFIG.UI.DASHBOARD_REFRESH_INTERVAL);
        }

        debugLog('Public dashboard manager setup completed');
    }

    async refreshData() {
        await Promise.all([
            this.refreshPublicSessions(),
            this.refreshPublicRooms()
        ]);
    }

    async refreshPublicSessions() {
        try {
            // Try to get sessions without authentication
            const response = await window.apiClient.getSessions(1, 50);

            DataListManager.renderList('public-sessions-list', response.items, (session) => {
                return this.renderPublicSessionCard(session);
            });

        } catch (error) {
            debugLog('Public sessions endpoint error:', error);
            const container = DOMUtils.$('#public-sessions-list');
            if (container) {
                const isNotFound = error.status === 404;
                container.innerHTML = `
                    <div class="empty-state">
                        <div class="empty-state-icon">${isNotFound ? '🔍' : '🔒'}</div>
                        <h3>${isNotFound ? 'Endpoint Not Found' : 'Authentication Required'}</h3>
                        <p>${isNotFound ?
                            'The sessions endpoint may not be implemented yet. Check the API Test tab for available endpoints.' :
                            'Session data requires authentication. Use the auth tab to login.'
                        }</p>
                    </div>
                `;
            }
        }
    }

    async refreshPublicRooms() {
        try {
            // Try to get rooms without authentication
            const response = await window.apiClient.getRooms(null, 1, 50);

            DataListManager.renderList('public-rooms-list', response.items, (room) => {
                return this.renderPublicRoomCard(room);
            });

        } catch (error) {
            debugLog('Public rooms endpoint error:', error);
            const container = DOMUtils.$('#public-rooms-list');
            if (container) {
                const isNotFound = error.status === 404;
                container.innerHTML = `
                    <div class="empty-state">
                        <div class="empty-state-icon">${isNotFound ? '🔍' : '🔒'}</div>
                        <h3>${isNotFound ? 'Endpoint Not Found' : 'Authentication Required'}</h3>
                        <p>${isNotFound ?
                            'The rooms endpoint may not be implemented yet. Check the API Test tab for available endpoints.' :
                            'Room data requires authentication. Use the auth tab to login.'
                        }</p>
                    </div>
                `;
            }
        }
    }

    renderPublicSessionCard(session) {
        const statusClass = session.status === 'Active' ? 'badge-success' :
                          session.status === 'Scheduled' ? 'badge-info' : 'badge-secondary';

        const content = `
            <div class="data-grid cols-2">
                <div>
                    <strong>Status:</strong>
                    <span class="badge ${statusClass}">${session.status}</span>
                </div>
                <div><strong>Participants:</strong> ${session.currentParticipants}/${session.maxParticipants}</div>
                <div><strong>Start Time:</strong> ${DateUtils.formatDate(session.scheduledStartTime)}</div>
                <div><strong>Duration:</strong> ${session.durationMinutes} minutes</div>
            </div>
        `;

        return DataListManager.createCard(session.name, content);
    }

    renderPublicRoomCard(room) {
        const content = `
            <div class="data-grid cols-2">
                <div><strong>Session:</strong> ${room.sessionName}</div>
                <div><strong>Participants:</strong> ${room.currentParticipants}/${room.maxParticipants}</div>
                <div><strong>Status:</strong> <span class="badge badge-info">${room.isActive ? 'Active' : 'Inactive'}</span></div>
                <div><strong>Created:</strong> ${DateUtils.formatDate(room.createdAt)}</div>
            </div>
        `;

        return DataListManager.createCard(room.name, content);
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
}

// Export managers
window.DashboardManager = DashboardManager;
window.PublicDashboardManager = PublicDashboardManager;
window.ChatManager = ChatManager;