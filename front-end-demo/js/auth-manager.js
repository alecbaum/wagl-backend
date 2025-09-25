// Wagl Backend API Demo - Authentication Manager

class AuthManager {
    constructor() {
        this.checkInterval = null;
        this.isRefreshing = false;
        this.refreshPromise = null;

        // Initialize authentication state
        this.initializeAuth();
        this.startTokenCheck();
    }

    // Initialize authentication state from storage
    initializeAuth() {
        const accessToken = StorageUtils.getItem(CONFIG.AUTH.ACCESS_TOKEN_KEY);
        const refreshToken = StorageUtils.getItem(CONFIG.AUTH.REFRESH_TOKEN_KEY);
        const userInfo = StorageUtils.getItem(CONFIG.AUTH.USER_INFO_KEY);

        if (accessToken && refreshToken && userInfo) {
            // Validate token format and expiry
            try {
                const tokenData = this.parseJwtToken(accessToken);
                const now = Date.now() / 1000;

                if (tokenData.exp > now) {
                    // Token is still valid
                    this.setAuthState(accessToken, refreshToken, userInfo, tokenData.exp);
                    debugLog('Authentication restored from storage');
                    return true;
                } else {
                    debugLog('Stored token expired, clearing auth state');
                    this.clearAuthState();
                }
            } catch (error) {
                errorLog(error, 'AuthManager.initializeAuth - token parsing');
                this.clearAuthState();
            }
        }

        return false;
    }

    // Parse JWT token to extract payload
    parseJwtToken(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) {
                throw new Error('Invalid JWT format');
            }

            const payload = parts[1];
            const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
            return JSON.parse(decoded);
        } catch (error) {
            throw new Error('Failed to parse JWT token');
        }
    }

    // Set authentication state
    setAuthState(accessToken, refreshToken, userInfo, expiresAt = null) {
        if (!expiresAt && accessToken) {
            try {
                const tokenData = this.parseJwtToken(accessToken);
                expiresAt = tokenData.exp;
            } catch (error) {
                errorLog(error, 'AuthManager.setAuthState - token parsing');
            }
        }

        // Update app state
        window.APP_STATE.tokens.accessToken = accessToken;
        window.APP_STATE.tokens.refreshToken = refreshToken;
        window.APP_STATE.tokens.expiresAt = expiresAt;
        window.APP_STATE.currentUser = userInfo;

        // Store in localStorage
        StorageUtils.setItem(CONFIG.AUTH.ACCESS_TOKEN_KEY, accessToken);
        StorageUtils.setItem(CONFIG.AUTH.REFRESH_TOKEN_KEY, refreshToken);
        StorageUtils.setItem(CONFIG.AUTH.USER_INFO_KEY, userInfo);

        debugLog('Authentication state updated', {
            user: userInfo?.email,
            expiresAt: expiresAt ? new Date(expiresAt * 1000) : 'unknown'
        });

        // Update UI
        this.updateAuthUI();
    }

    // Clear authentication state
    clearAuthState() {
        // Clear app state
        window.APP_STATE.tokens.accessToken = null;
        window.APP_STATE.tokens.refreshToken = null;
        window.APP_STATE.tokens.expiresAt = null;
        window.APP_STATE.currentUser = null;

        // Clear storage
        StorageUtils.clearAppData();

        debugLog('Authentication state cleared');

        // Update UI
        this.updateAuthUI();
    }

    // Check if user is authenticated
    isAuthenticated() {
        const token = window.APP_STATE.tokens.accessToken;
        const expiresAt = window.APP_STATE.tokens.expiresAt;

        if (!token) return false;

        // Check if token is expired
        if (expiresAt) {
            const now = Date.now() / 1000;
            return expiresAt > now;
        }

        // If no expiry info, try to parse token
        try {
            const tokenData = this.parseJwtToken(token);
            return tokenData.exp > (Date.now() / 1000);
        } catch (error) {
            return false;
        }
    }

    // Get current user info
    getCurrentUser() {
        return window.APP_STATE.currentUser;
    }

    // Login user
    async login(email, password, rememberMe = false) {
        try {
            const response = await window.apiClient.login(email, password, rememberMe);

            if (response.accessToken && response.refreshToken && response.user) {
                this.setAuthState(
                    response.accessToken,
                    response.refreshToken,
                    response.user
                );

                ToastManager.showSuccess('Login successful', `Welcome back, ${response.user.firstName}!`);

                // Navigate to dashboard
                window.pageManager.showPage('dashboard');

                return response;
            } else {
                throw new Error('Invalid response format');
            }
        } catch (error) {
            const friendlyMessage = getFriendlyErrorMessage(error);
            ToastManager.showError('Login failed', friendlyMessage);
            throw error;
        }
    }

    // Register user
    async register(userData) {
        try {
            const response = await window.apiClient.register(userData);

            if (response.accessToken && response.refreshToken && response.user) {
                this.setAuthState(
                    response.accessToken,
                    response.refreshToken,
                    response.user
                );

                ToastManager.showSuccess('Registration successful', `Welcome to Wagl, ${response.user.firstName}!`);

                // Navigate to dashboard
                window.pageManager.showPage('dashboard');

                return response;
            } else {
                throw new Error('Invalid response format');
            }
        } catch (error) {
            const friendlyMessage = getFriendlyErrorMessage(error);
            ToastManager.showError('Registration failed', friendlyMessage);
            throw error;
        }
    }

    // Join anonymously with invite
    async joinAnonymous(inviteCode, displayName, email = null) {
        try {
            const response = await window.apiClient.joinWithInvite(inviteCode, displayName, email);

            if (response.participantId && response.session && response.room) {
                // Store anonymous session info
                window.APP_STATE.chat.participantId = response.participantId;
                window.APP_STATE.ui.selectedSession = response.session;
                window.APP_STATE.ui.selectedRoom = response.room;

                // Set temporary user info
                window.APP_STATE.currentUser = {
                    displayName,
                    email,
                    isAnonymous: true,
                    participantId: response.participantId
                };

                ToastManager.showSuccess('Joined successfully', `Welcome to ${response.session.name}!`);

                // Navigate to anonymous chat
                window.pageManager.showPage('anonymous-chat');

                return response;
            } else {
                throw new Error('Invalid response format');
            }
        } catch (error) {
            const friendlyMessage = getFriendlyErrorMessage(error);
            ToastManager.showError('Join failed', friendlyMessage);
            throw error;
        }
    }

    // Logout user
    async logout() {
        try {
            // Call logout endpoint if authenticated
            if (this.isAuthenticated()) {
                await window.apiClient.logout();
            }
        } catch (error) {
            errorLog(error, 'AuthManager.logout - API call failed');
            // Continue with logout even if API call fails
        } finally {
            this.clearAuthState();
            ToastManager.showInfo('Logged out', 'You have been logged out successfully');

            // Navigate to auth page
            window.pageManager.showPage('auth');

            // Disconnect SignalR
            if (window.signalRManager) {
                window.signalRManager.disconnect();
            }
        }
    }

    // Refresh access token
    async refreshToken() {
        if (this.isRefreshing) {
            return this.refreshPromise;
        }

        this.isRefreshing = true;
        this.refreshPromise = this._performTokenRefresh();

        try {
            return await this.refreshPromise;
        } finally {
            this.isRefreshing = false;
            this.refreshPromise = null;
        }
    }

    async _performTokenRefresh() {
        try {
            const response = await window.apiClient.refreshToken();

            if (response.accessToken && response.refreshToken) {
                this.setAuthState(
                    response.accessToken,
                    response.refreshToken,
                    response.user || window.APP_STATE.currentUser
                );

                debugLog('Token refreshed successfully');
                return response;
            } else {
                throw new Error('Invalid refresh response');
            }
        } catch (error) {
            errorLog(error, 'AuthManager.refreshToken');

            // If refresh fails, clear auth state and redirect to login
            this.clearAuthState();
            ToastManager.showWarning('Session expired', 'Please login again');
            window.pageManager.showPage('auth');

            throw error;
        }
    }

    // Start automatic token checking
    startTokenCheck() {
        if (this.checkInterval) {
            clearInterval(this.checkInterval);
        }

        this.checkInterval = setInterval(() => {
            this.checkTokenExpiry();
        }, CONFIG.AUTH.TOKEN_CHECK_INTERVAL);

        debugLog('Token check started');
    }

    // Stop automatic token checking
    stopTokenCheck() {
        if (this.checkInterval) {
            clearInterval(this.checkInterval);
            this.checkInterval = null;
        }

        debugLog('Token check stopped');
    }

    // Check if token needs refresh
    checkTokenExpiry() {
        if (!this.isAuthenticated()) {
            return;
        }

        const expiresAt = window.APP_STATE.tokens.expiresAt;
        if (!expiresAt) return;

        const now = Date.now() / 1000;
        const timeUntilExpiry = expiresAt - now;

        // Refresh if token expires within the buffer time
        const refreshBuffer = CONFIG.AUTH.REFRESH_BUFFER / 1000; // Convert to seconds

        if (timeUntilExpiry <= refreshBuffer && timeUntilExpiry > 0) {
            debugLog('Token approaching expiry, refreshing...');
            this.refreshToken().catch(error => {
                errorLog(error, 'AuthManager.checkTokenExpiry - refresh failed');
            });
        }
    }

    // Update authentication UI elements
    updateAuthUI() {
        const userInfo = DOMUtils.$('#user-info');
        const userName = DOMUtils.$('#user-name');
        const currentUser = this.getCurrentUser();

        if (this.isAuthenticated() && currentUser && !currentUser.isAnonymous) {
            // Show user info in nav
            if (userInfo) DOMUtils.show(userInfo);
            if (userName) userName.textContent = currentUser.firstName || currentUser.displayName;
        } else {
            // Hide user info in nav
            if (userInfo) DOMUtils.hide(userInfo);
        }

        // Update page visibility
        if (this.isAuthenticated() && !currentUser?.isAnonymous) {
            window.pageManager?.showPage('dashboard');
        } else if (currentUser?.isAnonymous) {
            window.pageManager?.showPage('anonymous-chat');
        } else {
            window.pageManager?.showPage('auth');
        }
    }

    // Get authorization header value
    getAuthHeader() {
        const token = window.APP_STATE.tokens.accessToken;
        return token ? `Bearer ${token}` : null;
    }
}

// Export authentication manager
window.authManager = new AuthManager();