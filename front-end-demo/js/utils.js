// Wagl Backend API Demo - Utility Functions

// Date and Time Utilities
const DateUtils = {
    // Format date for display
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    // Format date for datetime-local input
    formatForInput(dateString) {
        const date = new Date(dateString);
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hours}:${minutes}`;
    },

    // Get relative time (e.g., "2 minutes ago")
    getRelativeTime(dateString) {
        const now = new Date();
        const date = new Date(dateString);
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);

        if (diffMins < 1) return 'just now';
        if (diffMins < 60) return `${diffMins} minute${diffMins !== 1 ? 's' : ''} ago`;
        if (diffHours < 24) return `${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
        return `${diffDays} day${diffDays !== 1 ? 's' : ''} ago`;
    },

    // Check if date is in the future
    isFuture(dateString) {
        return new Date(dateString) > new Date();
    },

    // Check if date is in the past
    isPast(dateString) {
        return new Date(dateString) < new Date();
    }
};

// String Utilities
const StringUtils = {
    // Truncate string with ellipsis
    truncate(str, maxLength) {
        if (!str || str.length <= maxLength) return str;
        return str.substring(0, maxLength - 3) + '...';
    },

    // Capitalize first letter
    capitalize(str) {
        if (!str) return str;
        return str.charAt(0).toUpperCase() + str.slice(1);
    },

    // Generate a random string
    randomString(length = 10) {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        let result = '';
        for (let i = 0; i < length; i++) {
            result += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return result;
    },

    // Generate UUID v4
    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },

    // Extract domain from email
    extractDomain(email) {
        if (!email || !email.includes('@')) return '';
        return email.split('@')[1];
    },

    // Format bytes to human readable
    formatBytes(bytes, decimals = 2) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const dm = decimals < 0 ? 0 : decimals;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    }
};

// DOM Utilities
const DOMUtils = {
    // Query selector with null check
    $(selector) {
        return document.querySelector(selector);
    },

    // Query all selectors
    $$(selector) {
        return Array.from(document.querySelectorAll(selector));
    },

    // Add event listener with automatic cleanup
    on(element, event, handler, options = {}) {
        if (typeof element === 'string') {
            element = this.$(element);
        }
        if (element) {
            element.addEventListener(event, handler, options);
            return () => element.removeEventListener(event, handler, options);
        }
        return () => {};
    },

    // Show element
    show(element) {
        if (typeof element === 'string') {
            element = this.$(element);
        }
        if (element) {
            element.style.display = '';
            element.classList.remove('hidden');
        }
    },

    // Hide element
    hide(element) {
        if (typeof element === 'string') {
            element = this.$(element);
        }
        if (element) {
            element.style.display = 'none';
            element.classList.add('hidden');
        }
    },

    // Toggle element visibility
    toggle(element) {
        if (typeof element === 'string') {
            element = this.$(element);
        }
        if (element) {
            if (element.style.display === 'none' || element.classList.contains('hidden')) {
                this.show(element);
            } else {
                this.hide(element);
            }
        }
    },

    // Create element with attributes and content
    create(tag, attributes = {}, content = '') {
        const element = document.createElement(tag);

        Object.entries(attributes).forEach(([key, value]) => {
            if (key === 'class') {
                element.className = value;
            } else if (key === 'data') {
                Object.entries(value).forEach(([dataKey, dataValue]) => {
                    element.dataset[dataKey] = dataValue;
                });
            } else {
                element.setAttribute(key, value);
            }
        });

        if (content) {
            if (typeof content === 'string') {
                element.innerHTML = content;
            } else {
                element.appendChild(content);
            }
        }

        return element;
    },

    // Scroll to element
    scrollTo(element, behavior = 'smooth') {
        if (typeof element === 'string') {
            element = this.$(element);
        }
        if (element) {
            element.scrollIntoView({ behavior, block: 'nearest' });
        }
    }
};

// Validation Utilities
const ValidationUtils = {
    // Email validation
    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    },

    // Password validation
    isValidPassword(password) {
        return password && password.length >= CONFIG.VALIDATION.PASSWORD_MIN_LENGTH;
    },

    // Invite code validation
    isValidInviteCode(code) {
        return code && code.length >= CONFIG.VALIDATION.INVITE_CODE_MIN_LENGTH;
    },

    // Session name validation
    isValidSessionName(name) {
        return name && name.trim().length > 0 && name.length <= CONFIG.VALIDATION.SESSION_NAME_MAX_LENGTH;
    },

    // Display name validation
    isValidDisplayName(name) {
        return name && name.trim().length > 0 && name.length <= CONFIG.VALIDATION.DISPLAY_NAME_MAX_LENGTH;
    },

    // Message content validation
    isValidMessage(content) {
        return content && content.trim().length > 0 && content.length <= CONFIG.VALIDATION.MESSAGE_MAX_LENGTH;
    }
};

// Storage Utilities
const StorageUtils = {
    // Set item in localStorage with JSON serialization
    setItem(key, value) {
        try {
            const serialized = JSON.stringify(value);
            localStorage.setItem(key, serialized);
            return true;
        } catch (error) {
            errorLog(error, 'StorageUtils.setItem');
            return false;
        }
    },

    // Get item from localStorage with JSON parsing
    getItem(key, defaultValue = null) {
        try {
            const item = localStorage.getItem(key);
            if (item === null) return defaultValue;
            return JSON.parse(item);
        } catch (error) {
            errorLog(error, 'StorageUtils.getItem');
            return defaultValue;
        }
    },

    // Remove item from localStorage
    removeItem(key) {
        try {
            localStorage.removeItem(key);
            return true;
        } catch (error) {
            errorLog(error, 'StorageUtils.removeItem');
            return false;
        }
    },

    // Clear all app-related items from localStorage
    clearAppData() {
        try {
            const keysToRemove = [
                CONFIG.AUTH.ACCESS_TOKEN_KEY,
                CONFIG.AUTH.REFRESH_TOKEN_KEY,
                CONFIG.AUTH.USER_INFO_KEY
            ];
            keysToRemove.forEach(key => localStorage.removeItem(key));
            return true;
        } catch (error) {
            errorLog(error, 'StorageUtils.clearAppData');
            return false;
        }
    }
};

// HTTP Utilities
const HttpUtils = {
    // Create request headers with authentication
    getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth && window.APP_STATE.tokens.accessToken) {
            headers['Authorization'] = `Bearer ${window.APP_STATE.tokens.accessToken}`;
        }

        return headers;
    },

    // Handle HTTP errors
    async handleResponse(response) {
        // Track request count for rate limiting awareness
        window.APP_STATE.requests.count++;

        // Update rate limit info from headers
        if (response.headers.get('X-RateLimit-Limit')) {
            debugLog('Rate limit info:', {
                limit: response.headers.get('X-RateLimit-Limit'),
                remaining: response.headers.get('X-RateLimit-Remaining'),
                reset: response.headers.get('X-RateLimit-Reset')
            });
        }

        if (!response.ok) {
            let errorData;
            try {
                errorData = await response.json();
            } catch {
                errorData = { message: response.statusText, status: response.status };
            }

            // Enhanced error logging for debugging
            debugLog('HTTP Error Response:', {
                status: response.status,
                statusText: response.statusText,
                errorData: errorData,
                url: response.url
            });

            const error = new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
            error.status = response.status;
            error.data = errorData;
            throw error;
        }

        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        }

        return await response.text();
    },

    // Copy text to clipboard
    async copyToClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (error) {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();

            try {
                document.execCommand('copy');
                document.body.removeChild(textArea);
                return true;
            } catch (err) {
                document.body.removeChild(textArea);
                return false;
            }
        }
    }
};

// Animation Utilities
const AnimationUtils = {
    // Fade in element
    fadeIn(element, duration = 300) {
        if (typeof element === 'string') {
            element = DOMUtils.$(element);
        }
        if (!element) return;

        element.style.opacity = '0';
        element.style.display = '';

        let start = null;
        function animate(timestamp) {
            if (!start) start = timestamp;
            const progress = (timestamp - start) / duration;

            if (progress < 1) {
                element.style.opacity = progress;
                requestAnimationFrame(animate);
            } else {
                element.style.opacity = '1';
            }
        }

        requestAnimationFrame(animate);
    },

    // Fade out element
    fadeOut(element, duration = 300) {
        if (typeof element === 'string') {
            element = DOMUtils.$(element);
        }
        if (!element) return;

        let start = null;
        function animate(timestamp) {
            if (!start) start = timestamp;
            const progress = (timestamp - start) / duration;

            if (progress < 1) {
                element.style.opacity = 1 - progress;
                requestAnimationFrame(animate);
            } else {
                element.style.opacity = '0';
                element.style.display = 'none';
            }
        }

        requestAnimationFrame(animate);
    },

    // Slide down element
    slideDown(element, duration = 300) {
        if (typeof element === 'string') {
            element = DOMUtils.$(element);
        }
        if (!element) return;

        element.style.maxHeight = '0';
        element.style.overflow = 'hidden';
        element.style.display = '';

        const height = element.scrollHeight;
        let start = null;

        function animate(timestamp) {
            if (!start) start = timestamp;
            const progress = (timestamp - start) / duration;

            if (progress < 1) {
                element.style.maxHeight = (height * progress) + 'px';
                requestAnimationFrame(animate);
            } else {
                element.style.maxHeight = '';
                element.style.overflow = '';
            }
        }

        requestAnimationFrame(animate);
    }
};

// Debounce utility
function debounce(func, wait, immediate = false) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            timeout = null;
            if (!immediate) func.apply(this, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(this, args);
    };
}

// Throttle utility
function throttle(func, limit) {
    let inThrottle;
    return function(...args) {
        if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Format number with commas
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

// Get user-friendly error message
function getFriendlyErrorMessage(error) {
    if (!error) return 'An unknown error occurred';

    const errorCode = error.data?.error || error.code;
    const errorMessages = {
        [CONFIG.ERROR_CODES.INVALID_CREDENTIALS]: 'Invalid email or password',
        [CONFIG.ERROR_CODES.INVALID_TOKEN]: 'Session expired, please login again',
        [CONFIG.ERROR_CODES.TOKEN_EXPIRED]: 'Session expired, please login again',
        [CONFIG.ERROR_CODES.UNAUTHORIZED]: 'You are not authorized to perform this action',
        [CONFIG.ERROR_CODES.SESSION_NOT_FOUND]: 'Session not found',
        [CONFIG.ERROR_CODES.ROOM_FULL]: 'Room is full, please try again later',
        [CONFIG.ERROR_CODES.INVITE_NOT_FOUND]: 'Invite code not found or expired',
        [CONFIG.ERROR_CODES.NETWORK_ERROR]: 'Network error, please check your connection'
    };

    return errorMessages[errorCode] || error.message || 'An unexpected error occurred';
}

// Export utilities for global use
window.DateUtils = DateUtils;
window.StringUtils = StringUtils;
window.DOMUtils = DOMUtils;
window.ValidationUtils = ValidationUtils;
window.StorageUtils = StorageUtils;
window.HttpUtils = HttpUtils;
window.AnimationUtils = AnimationUtils;
window.debounce = debounce;
window.throttle = throttle;
window.formatNumber = formatNumber;
window.getFriendlyErrorMessage = getFriendlyErrorMessage;