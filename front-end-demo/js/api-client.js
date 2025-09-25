// Wagl Backend API Demo - API Client

class ApiClient {
    constructor() {
        this.retryCount = 0;
        this.maxRetries = CONFIG.API.RETRY_ATTEMPTS;
    }

    get baseUrl() {
        // Force direct URL to avoid SSL certificate issues
        const endpoint = window.APP_STATE.apiEndpoint;
        if (endpoint.includes('api.wagl.ai')) {
            debugLog('WARNING: Forcing direct URL to avoid SSL issues');
            window.APP_STATE.apiEndpoint = 'https://v6uwnty3vi.us-east-1.awsapprunner.com';
            return window.APP_STATE.apiEndpoint;
        }
        return endpoint;
    }

    // Switch between primary and direct endpoints
    switchEndpoint() {
        const newEndpoint = window.APP_STATE.apiEndpoint === CONFIG.API.PRIMARY_BASE_URL
            ? CONFIG.API.DIRECT_BASE_URL
            : CONFIG.API.PRIMARY_BASE_URL;

        window.APP_STATE.apiEndpoint = newEndpoint;

        debugLog('Switched API endpoint to:', newEndpoint);
        return newEndpoint;
    }

    // Make HTTP request with retry logic
    async request(endpoint, options = {}) {
        debugLog('ApiClient.request - APP_STATE.apiEndpoint:', window.APP_STATE.apiEndpoint);
        const url = getApiUrl(endpoint);
        const config = {
            method: 'GET',
            headers: this.getRequestHeaders(options.useApiKey),
            ...options
        };

        debugLog('API Request:', {
            url,
            method: config.method,
            headers: config.headers,
            body: config.body ? config.body : undefined,
            bodyParsed: config.body ? JSON.parse(config.body) : undefined
        });

        try {
            const response = await fetch(url, config);
            const data = await HttpUtils.handleResponse(response);

            this.retryCount = 0; // Reset on success
            return data;
        } catch (error) {
            errorLog(error, `ApiClient.request(${endpoint})`);

            // Retry logic for network errors
            if (this.retryCount < this.maxRetries &&
                (error.code === 'NETWORK_ERROR' || error.status >= 500)) {

                this.retryCount++;
                debugLog(`Retrying request (${this.retryCount}/${this.maxRetries}) after delay`);

                await new Promise(resolve =>
                    setTimeout(resolve, CONFIG.API.RETRY_DELAY * this.retryCount)
                );

                return this.request(endpoint, options);
            }

            throw error;
        }
    }

    // Get request headers with proper authentication
    getRequestHeaders(useApiKey = false) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (useApiKey && window.APP_STATE.moderator.apiKey) {
            // Use API key for moderator authentication
            headers['Authorization'] = `Bearer ${window.APP_STATE.moderator.apiKey}`;
        } else if (window.APP_STATE.tokens.accessToken) {
            // Use JWT token for user authentication
            headers['Authorization'] = `Bearer ${window.APP_STATE.tokens.accessToken}`;
        }

        return headers;
    }

    // Authentication endpoints
    async login(email, password, rememberMe = false) {
        return this.request('/auth/login', {
            method: 'POST',
            headers: HttpUtils.getHeaders(false),
            body: JSON.stringify({
                email,
                password,
                rememberMe
            })
        });
    }

    async register(userData) {
        return this.request('/auth/register', {
            method: 'POST',
            headers: HttpUtils.getHeaders(false),
            body: JSON.stringify(userData)
        });
    }

    async refreshToken() {
        const refreshToken = StorageUtils.getItem(CONFIG.AUTH.REFRESH_TOKEN_KEY);
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }

        return this.request('/auth/refresh', {
            method: 'POST',
            headers: HttpUtils.getHeaders(false),
            body: JSON.stringify({ refreshToken })
        });
    }

    async logout() {
        return this.request('/auth/logout', {
            method: 'POST'
        });
    }

    // Session management endpoints
    async getSessions(page = 1, pageSize = CONFIG.UI.DEFAULT_PAGE_SIZE) {
        return this.request(`/sessions?page=${page}&pageSize=${pageSize}`);
    }

    async getSession(sessionId) {
        return this.request(`/sessions/${sessionId}`);
    }

    async createSession(sessionData) {
        return this.request('/sessions', {
            method: 'POST',
            body: JSON.stringify(sessionData)
        });
    }

    async updateSession(sessionId, sessionData) {
        return this.request(`/sessions/${sessionId}`, {
            method: 'PUT',
            body: JSON.stringify(sessionData)
        });
    }

    async deleteSession(sessionId) {
        return this.request(`/sessions/${sessionId}`, {
            method: 'DELETE'
        });
    }

    async startSession(sessionId) {
        return this.request(`/sessions/${sessionId}/start`, {
            method: 'POST'
        });
    }

    async endSession(sessionId) {
        return this.request(`/sessions/${sessionId}/end`, {
            method: 'POST'
        });
    }

    // Room management endpoints
    async getRooms(sessionId = null, page = 1, pageSize = CONFIG.UI.DEFAULT_PAGE_SIZE) {
        const params = new URLSearchParams({ page, pageSize });
        if (sessionId) params.append('sessionId', sessionId);

        return this.request(`/rooms?${params}`);
    }

    async getRoom(roomId) {
        return this.request(`/rooms/${roomId}`);
    }

    async createRoom(roomData) {
        return this.request('/rooms', {
            method: 'POST',
            body: JSON.stringify(roomData)
        });
    }

    async updateRoom(roomId, roomData) {
        return this.request(`/rooms/${roomId}`, {
            method: 'PUT',
            body: JSON.stringify(roomData)
        });
    }

    async deleteRoom(roomId) {
        return this.request(`/rooms/${roomId}`, {
            method: 'DELETE'
        });
    }

    async joinRoom(roomId, displayName) {
        return this.request(`/rooms/${roomId}/join`, {
            method: 'POST',
            body: JSON.stringify({ displayName })
        });
    }

    async leaveRoom(roomId, participantId) {
        return this.request(`/rooms/${roomId}/leave`, {
            method: 'POST',
            body: JSON.stringify({ participantId })
        });
    }

    async getRoomParticipants(roomId) {
        return this.request(`/rooms/${roomId}/participants`);
    }

    async getRoomMessages(roomId, page = 1, pageSize = CONFIG.UI.MESSAGE_LOAD_COUNT) {
        return this.request(`/rooms/${roomId}/messages?page=${page}&pageSize=${pageSize}`);
    }

    // Invite management endpoints
    async getInvites(sessionId = null, page = 1, pageSize = CONFIG.UI.DEFAULT_PAGE_SIZE) {
        const params = new URLSearchParams({ page, pageSize });
        if (sessionId) params.append('sessionId', sessionId);

        return this.request(`/invites?${params}`);
    }

    async generateInvite(sessionId, expirationMinutes = 120) {
        return this.request('/invites', {
            method: 'POST',
            body: JSON.stringify({
                sessionId,
                expirationMinutes
            })
        });
    }

    async validateInvite(inviteCode) {
        return this.request('/invites/validate', {
            method: 'POST',
            headers: HttpUtils.getHeaders(false),
            body: JSON.stringify({ inviteCode })
        });
    }

    async joinWithInvite(inviteCode, displayName, email = null) {
        return this.request('/invites/join', {
            method: 'POST',
            headers: HttpUtils.getHeaders(false),
            body: JSON.stringify({
                inviteCode,
                displayName,
                email
            })
        });
    }

    async revokeInvite(inviteId) {
        return this.request(`/invites/${inviteId}`, {
            method: 'DELETE'
        });
    }

    // Dashboard and analytics endpoints
    async getDashboardStats() {
        return this.request('/dashboard/stats');
    }

    async getAnalytics(dateRange = '7d') {
        return this.request(`/analytics?range=${dateRange}`);
    }

    // User management endpoints
    async getUsers(page = 1, pageSize = CONFIG.UI.DEFAULT_PAGE_SIZE) {
        return this.request(`/users?page=${page}&pageSize=${pageSize}`);
    }

    async getUser(userId) {
        return this.request(`/users/${userId}`);
    }

    async updateUser(userId, userData) {
        return this.request(`/users/${userId}`, {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    async deleteUser(userId) {
        return this.request(`/users/${userId}`, {
            method: 'DELETE'
        });
    }

    async getCurrentUser() {
        return this.request('/users/me');
    }

    async updateProfile(userData) {
        return this.request('/users/me', {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    // Message endpoints
    async sendMessage(roomId, content, messageType = 'Text') {
        return this.request(`/rooms/${roomId}/messages`, {
            method: 'POST',
            body: JSON.stringify({
                content,
                messageType
            })
        });
    }

    // Moderator/Provider management endpoints
    async getProviders(page = 1, pageSize = CONFIG.UI.DEFAULT_PAGE_SIZE) {
        return this.request(`/providers?page=${page}&pageSize=${pageSize}`);
    }

    async createProvider(providerData) {
        return this.request('/providers', {
            method: 'POST',
            body: JSON.stringify(providerData)
        });
    }

    async updateProvider(providerId, providerData) {
        return this.request(`/providers/${providerId}`, {
            method: 'PUT',
            body: JSON.stringify(providerData)
        });
    }

    async deleteProvider(providerId) {
        return this.request(`/providers/${providerId}`, {
            method: 'DELETE'
        });
    }

    async regenerateApiKey(providerId) {
        return this.request(`/providers/${providerId}/regenerate-key`, {
            method: 'POST'
        });
    }

    // Moderator API key authentication
    async authenticateWithApiKey(apiKey) {
        window.APP_STATE.moderator.apiKey = apiKey;
        window.APP_STATE.moderator.isActive = true;

        try {
            // Test the API key by getting provider info
            const response = await this.request('/providers/me', { useApiKey: true });
            window.APP_STATE.moderator.provider = response;
            return response;
        } catch (error) {
            window.APP_STATE.moderator.apiKey = null;
            window.APP_STATE.moderator.isActive = false;
            window.APP_STATE.moderator.provider = null;
            throw error;
        }
    }

    // Moderator operations (use API key)
    async moderatorJoinRoom(roomId, displayName = 'Moderator') {
        return this.request(`/rooms/${roomId}/join`, {
            method: 'POST',
            body: JSON.stringify({ displayName }),
            useApiKey: true
        });
    }

    async moderatorSendMessage(roomId, content, messageType = 'Text') {
        return this.request(`/rooms/${roomId}/messages`, {
            method: 'POST',
            body: JSON.stringify({ content, messageType }),
            useApiKey: true
        });
    }

    async moderatorGetRooms(sessionId = null) {
        const params = sessionId ? `?sessionId=${sessionId}` : '';
        return this.request(`/rooms${params}`, { useApiKey: true });
    }

    // Health check endpoint
    async healthCheck() {
        try {
            const response = await fetch(`${this.baseUrl}/health`, {
                method: 'GET',
                timeout: 5000
            });
            return response.ok;
        } catch (error) {
            errorLog(error, 'ApiClient.healthCheck');
            return false;
        }
    }
}

// Export API client instance
window.apiClient = new ApiClient();