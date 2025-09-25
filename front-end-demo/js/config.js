// Wagl Backend API Demo - Configuration

const CONFIG = {
    // API Configuration
    API: {
        // Primary endpoint (custom domain)
        PRIMARY_BASE_URL: 'https://api.wagl.ai',
        // Direct App Runner endpoint (backup)
        DIRECT_BASE_URL: 'https://v6uwnty3vi.us-east-1.awsapprunner.com',
        // API version
        VERSION: 'v1.0',
        // Default timeout for requests
        TIMEOUT: 30000,
        // Retry configuration
        RETRY_ATTEMPTS: 3,
        RETRY_DELAY: 1000
    },

    // SignalR Configuration
    SIGNALR: {
        HUB_PATH: '/chathub',
        // Connection options
        OPTIONS: {
            // Automatic reconnection
            automaticReconnect: {
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.previousRetryCount === 0) {
                        return 0;
                    }
                    return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount - 1), 30000);
                }
            },
            // Logging level (for development)
            logger: signalR.LogLevel.Information
        }
    },

    // Authentication Configuration
    AUTH: {
        // Token storage keys
        ACCESS_TOKEN_KEY: 'wagl_access_token',
        REFRESH_TOKEN_KEY: 'wagl_refresh_token',
        USER_INFO_KEY: 'wagl_user_info',
        // Token expiry check interval (5 minutes)
        TOKEN_CHECK_INTERVAL: 5 * 60 * 1000,
        // Refresh token before expiry (10 minutes)
        REFRESH_BUFFER: 10 * 60 * 1000
    },

    // UI Configuration
    UI: {
        // Toast notification duration
        TOAST_DURATION: 5000,
        // Auto-refresh intervals
        DASHBOARD_REFRESH_INTERVAL: 30000, // 30 seconds
        SESSION_REFRESH_INTERVAL: 10000,   // 10 seconds
        // Pagination
        DEFAULT_PAGE_SIZE: 20,
        MAX_PAGE_SIZE: 100,
        // Chat
        MAX_MESSAGES_DISPLAY: 100,
        MESSAGE_LOAD_COUNT: 50
    },

    // Feature Flags
    FEATURES: {
        // Enable debug logging
        DEBUG_MODE: true,
        // Enable mock data (for testing without backend)
        MOCK_MODE: false,
        // Enable advanced admin features
        ADMIN_FEATURES: true,
        // Enable real-time features
        REALTIME_FEATURES: true
    },

    // Rate Limiting (client-side awareness)
    RATE_LIMITS: {
        TIER1: 100,    // requests per hour
        TIER2: 500,    // requests per hour
        TIER3: 2000,   // requests per hour
        ANONYMOUS: 50  // requests per hour per IP
    },

    // Validation Rules
    VALIDATION: {
        PASSWORD_MIN_LENGTH: 8,
        SESSION_NAME_MAX_LENGTH: 100,
        DISPLAY_NAME_MAX_LENGTH: 50,
        MESSAGE_MAX_LENGTH: 2000,
        INVITE_CODE_MIN_LENGTH: 32
    },

    // Error Codes (matching backend)
    ERROR_CODES: {
        // Authentication
        INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
        INVALID_TOKEN: 'INVALID_TOKEN',
        TOKEN_EXPIRED: 'TOKEN_EXPIRED',
        UNAUTHORIZED: 'UNAUTHORIZED',
        INSUFFICIENT_PERMISSIONS: 'INSUFFICIENT_PERMISSIONS',

        // Sessions
        SESSION_NOT_FOUND: 'SESSION_NOT_FOUND',
        SESSION_NOT_ACTIVE: 'SESSION_NOT_ACTIVE',
        SESSION_EXPIRED: 'SESSION_EXPIRED',

        // Rooms
        ROOM_FULL: 'ROOM_FULL',
        ROOM_NOT_FOUND: 'ROOM_NOT_FOUND',

        // Invites
        INVITE_NOT_FOUND: 'INVITE_NOT_FOUND',
        INVITE_EXPIRED: 'INVITE_EXPIRED',
        INVALID_CODE_FORMAT: 'INVALID_CODE_FORMAT',

        // General
        VALIDATION_ERROR: 'VALIDATION_ERROR',
        INTERNAL_ERROR: 'INTERNAL_ERROR',
        NETWORK_ERROR: 'NETWORK_ERROR',
        JOIN_FAILED: 'JOIN_FAILED',
        DUPLICATE_EMAIL: 'DUPLICATE_EMAIL'
    },

    // Status Types
    STATUS: {
        SESSION: {
            SCHEDULED: 'Scheduled',
            ACTIVE: 'Active',
            ENDED: 'Ended',
            CANCELLED: 'Cancelled'
        },
        CONNECTION: {
            DISCONNECTED: 'Disconnected',
            CONNECTING: 'Connecting',
            CONNECTED: 'Connected',
            RECONNECTING: 'Reconnecting'
        }
    }
};

// Global state management
window.APP_STATE = {
    // Current user info
    currentUser: null,
    // Authentication tokens
    tokens: {
        accessToken: null,
        refreshToken: null,
        expiresAt: null
    },
    // Current API endpoint - FORCED to direct URL to avoid SSL cert issues
    apiEndpoint: 'https://v6uwnty3vi.us-east-1.awsapprunner.com',
    // SignalR connection
    signalRConnection: null,
    // UI state
    ui: {
        currentPage: 'auth',
        currentTab: 'login',
        selectedSession: null,
        selectedRoom: null,
        isLoading: false
    },
    // Chat state
    chat: {
        currentRoomId: null,
        participantId: null,
        messages: [],
        participants: []
    },
    // Moderator state (API key authentication)
    moderator: {
        apiKey: null,
        isActive: false,
        provider: null
    },
    // Request tracking (for rate limiting awareness)
    requests: {
        count: 0,
        resetTime: null
    }
};

// Utility function to get full API URL
function getApiUrl(endpoint) {
    const baseUrl = window.APP_STATE.apiEndpoint;
    const apiPath = `/api/${CONFIG.API.VERSION}`;
    const fullUrl = `${baseUrl}${apiPath}${endpoint}`;
    debugLog('getApiUrl:', { endpoint, baseUrl, fullUrl });
    return fullUrl;
}

// Utility function to get SignalR URL
function getSignalRUrl() {
    const baseUrl = window.APP_STATE.apiEndpoint;
    return `${baseUrl}${CONFIG.SIGNALR.HUB_PATH}`;
}

// Debug logging utility
function debugLog(...args) {
    if (CONFIG.FEATURES.DEBUG_MODE) {
        console.log('[Wagl Demo]', ...args);
    }
}

// Error logging utility
function errorLog(error, context) {
    console.error('[Wagl Demo Error]', context || '', error);

    // In production, you might want to send this to a logging service
    if (!CONFIG.FEATURES.DEBUG_MODE) {
        // Send to error tracking service
    }
}

// Export configuration for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { CONFIG, APP_STATE, getApiUrl, getSignalRUrl, debugLog, errorLog };
}