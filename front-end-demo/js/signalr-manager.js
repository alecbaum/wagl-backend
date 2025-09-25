// Wagl Backend API Demo - SignalR Manager

class SignalRManager {
    constructor() {
        this.connection = null;
        this.isConnecting = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.connectionState = CONFIG.STATUS.CONNECTION.DISCONNECTED;
        this.messageHandlers = new Map();
        this.roomSubscriptions = new Set();

        // Initialize connection if authenticated
        if (window.authManager && window.authManager.isAuthenticated()) {
            this.initialize();
        }
    }

    // Initialize SignalR connection
    initialize() {
        if (this.connection || this.isConnecting) {
            debugLog('SignalR already initialized or connecting');
            return;
        }

        try {
            const signalRUrl = getSignalRUrl();
            debugLog('Initializing SignalR connection to:', signalRUrl);

            // Create connection with authentication
            const connectionBuilder = new signalR.HubConnectionBuilder()
                .withUrl(signalRUrl, {
                    accessTokenFactory: () => {
                        const token = window.APP_STATE.tokens.accessToken;
                        if (!token) {
                            debugLog('No access token available for SignalR');
                            return null;
                        }
                        return token;
                    },
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect(CONFIG.SIGNALR.OPTIONS.automaticReconnect);

            // Add logging in debug mode
            if (CONFIG.FEATURES.DEBUG_MODE) {
                connectionBuilder.configureLogging(CONFIG.SIGNALR.OPTIONS.logger);
            }

            this.connection = connectionBuilder.build();

            // Setup event handlers
            this.setupEventHandlers();
            this.setupMessageHandlers();

            window.APP_STATE.signalRConnection = this.connection;

        } catch (error) {
            errorLog(error, 'SignalRManager.initialize');
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.DISCONNECTED);
        }
    }

    // Setup SignalR event handlers
    setupEventHandlers() {
        if (!this.connection) return;

        // Connection events
        this.connection.onclose(error => {
            debugLog('SignalR connection closed', error);
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.DISCONNECTED);
            this.clearRoomSubscriptions();

            if (error) {
                errorLog(error, 'SignalR connection closed with error');
            }
        });

        this.connection.onreconnecting(error => {
            debugLog('SignalR reconnecting', error);
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.RECONNECTING);
        });

        this.connection.onreconnected(connectionId => {
            debugLog('SignalR reconnected', connectionId);
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.CONNECTED);
            this.reconnectAttempts = 0;

            // Rejoin all previously subscribed rooms
            this.rejoinRooms();
        });
    }

    // Setup message handlers for different message types
    setupMessageHandlers() {
        if (!this.connection) return;

        // Receive messages
        this.connection.on('ReceiveMessage', (message) => {
            debugLog('Message received:', message);
            this.handleMessage('message', message);
        });

        // User joined room
        this.connection.on('UserJoined', (participant) => {
            debugLog('User joined:', participant);
            this.handleMessage('user-joined', participant);
        });

        // User left room
        this.connection.on('UserLeft', (participant) => {
            debugLog('User left:', participant);
            this.handleMessage('user-left', participant);
        });

        // Room status updates
        this.connection.on('RoomStatusChanged', (roomUpdate) => {
            debugLog('Room status changed:', roomUpdate);
            this.handleMessage('room-status', roomUpdate);
        });

        // Session status updates
        this.connection.on('SessionStatusChanged', (sessionUpdate) => {
            debugLog('Session status changed:', sessionUpdate);
            this.handleMessage('session-status', sessionUpdate);
        });

        // Participant list updates
        this.connection.on('ParticipantsUpdated', (participants) => {
            debugLog('Participants updated:', participants);
            this.handleMessage('participants-updated', participants);
        });

        // System notifications
        this.connection.on('SystemNotification', (notification) => {
            debugLog('System notification:', notification);
            this.handleMessage('system-notification', notification);
        });

        // Typing indicators
        this.connection.on('UserTyping', (typingInfo) => {
            debugLog('User typing:', typingInfo);
            this.handleMessage('user-typing', typingInfo);
        });

        this.connection.on('UserStoppedTyping', (typingInfo) => {
            debugLog('User stopped typing:', typingInfo);
            this.handleMessage('user-stopped-typing', typingInfo);
        });
    }

    // Handle incoming messages and route to registered handlers
    handleMessage(messageType, data) {
        const handlers = this.messageHandlers.get(messageType) || [];

        handlers.forEach(handler => {
            try {
                handler(data);
            } catch (error) {
                errorLog(error, `SignalR message handler for ${messageType}`);
            }
        });
    }

    // Register message handler
    onMessage(messageType, handler) {
        if (!this.messageHandlers.has(messageType)) {
            this.messageHandlers.set(messageType, []);
        }

        this.messageHandlers.get(messageType).push(handler);

        // Return unsubscribe function
        return () => {
            const handlers = this.messageHandlers.get(messageType);
            if (handlers) {
                const index = handlers.indexOf(handler);
                if (index !== -1) {
                    handlers.splice(index, 1);
                }
            }
        };
    }

    // Connect to SignalR hub
    async connect() {
        if (!this.connection) {
            this.initialize();
        }

        if (!this.connection || this.isConnecting) {
            return false;
        }

        if (this.connection.state === signalR.HubConnectionState.Connected) {
            debugLog('SignalR already connected');
            return true;
        }

        this.isConnecting = true;
        this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.CONNECTING);

        try {
            await this.connection.start();
            debugLog('SignalR connected successfully');
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.CONNECTED);
            this.reconnectAttempts = 0;
            return true;

        } catch (error) {
            errorLog(error, 'SignalRManager.connect');
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.DISCONNECTED);

            // Try to reconnect with delay
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnectAttempts++;
                const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts - 1), 30000);
                debugLog(`Retrying connection in ${delay}ms (attempt ${this.reconnectAttempts})`);

                setTimeout(() => {
                    this.connect();
                }, delay);
            }

            return false;
        } finally {
            this.isConnecting = false;
        }
    }

    // Disconnect from SignalR hub
    async disconnect() {
        if (!this.connection) {
            return;
        }

        try {
            await this.connection.stop();
            debugLog('SignalR disconnected');
        } catch (error) {
            errorLog(error, 'SignalRManager.disconnect');
        } finally {
            this.updateConnectionStatus(CONFIG.STATUS.CONNECTION.DISCONNECTED);
            this.clearRoomSubscriptions();
        }
    }

    // Join a chat room
    async joinRoom(roomId, displayName) {
        if (!this.isConnected()) {
            throw new Error('Not connected to SignalR hub');
        }

        try {
            await this.connection.invoke('JoinRoom', roomId, displayName);
            this.roomSubscriptions.add(roomId);
            debugLog('Joined room:', roomId);
            return true;

        } catch (error) {
            errorLog(error, `SignalRManager.joinRoom(${roomId})`);
            throw error;
        }
    }

    // Leave a chat room
    async leaveRoom(roomId) {
        if (!this.isConnected()) {
            return;
        }

        try {
            await this.connection.invoke('LeaveRoom', roomId);
            this.roomSubscriptions.delete(roomId);
            debugLog('Left room:', roomId);
            return true;

        } catch (error) {
            errorLog(error, `SignalRManager.leaveRoom(${roomId})`);
            throw error;
        }
    }

    // Send message to room
    async sendMessage(roomId, content, messageType = 'Text') {
        if (!this.isConnected()) {
            throw new Error('Not connected to SignalR hub');
        }

        try {
            await this.connection.invoke('SendMessage', roomId, content, messageType);
            debugLog('Message sent to room:', roomId, content);
            return true;

        } catch (error) {
            errorLog(error, `SignalRManager.sendMessage(${roomId})`);
            throw error;
        }
    }

    // Send typing indicator
    async sendTyping(roomId, isTyping = true) {
        if (!this.isConnected()) {
            return;
        }

        try {
            if (isTyping) {
                await this.connection.invoke('StartTyping', roomId);
            } else {
                await this.connection.invoke('StopTyping', roomId);
            }
        } catch (error) {
            errorLog(error, `SignalRManager.sendTyping(${roomId})`);
        }
    }

    // Get current connection state
    getConnectionState() {
        if (!this.connection) {
            return signalR.HubConnectionState.Disconnected;
        }

        return this.connection.state;
    }

    // Check if connected
    isConnected() {
        return this.getConnectionState() === signalR.HubConnectionState.Connected;
    }

    // Update connection status in UI
    updateConnectionStatus(status) {
        this.connectionState = status;

        const statusIndicator = DOMUtils.$('#status-indicator');
        const statusText = DOMUtils.$('#status-text');

        if (statusIndicator) {
            statusIndicator.className = 'status-indicator';
            statusIndicator.classList.add(status.toLowerCase());
        }

        if (statusText) {
            statusText.textContent = status;
        }

        // Update app state
        window.APP_STATE.ui.connectionStatus = status;

        debugLog('Connection status updated:', status);
    }

    // Rejoin all previously subscribed rooms after reconnection
    async rejoinRooms() {
        if (!this.isConnected() || this.roomSubscriptions.size === 0) {
            return;
        }

        debugLog('Rejoining rooms after reconnection:', Array.from(this.roomSubscriptions));

        for (const roomId of this.roomSubscriptions) {
            try {
                const currentUser = window.authManager.getCurrentUser();
                const displayName = currentUser?.displayName || currentUser?.firstName || 'Anonymous';
                await this.joinRoom(roomId, displayName);
            } catch (error) {
                errorLog(error, `Failed to rejoin room ${roomId}`);
                // Remove from subscriptions if rejoin fails
                this.roomSubscriptions.delete(roomId);
            }
        }
    }

    // Clear all room subscriptions
    clearRoomSubscriptions() {
        this.roomSubscriptions.clear();
        debugLog('Room subscriptions cleared');
    }

    // Get list of subscribed rooms
    getSubscribedRooms() {
        return Array.from(this.roomSubscriptions);
    }

    // Clean up resources
    destroy() {
        this.disconnect();
        this.messageHandlers.clear();
        this.clearRoomSubscriptions();

        if (this.connection) {
            this.connection = null;
            window.APP_STATE.signalRConnection = null;
        }
    }
}

// Export SignalR manager
window.signalRManager = new SignalRManager();