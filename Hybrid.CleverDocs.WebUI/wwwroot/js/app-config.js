/**
 * Application Configuration
 * Centralized configuration for frontend JavaScript modules
 */

window.appConfig = {
    // API Base URL - configurable per environment
    apiBaseUrl: window.location.protocol + '//' + window.location.hostname + ':5253',
    
    // SignalR Configuration
    signalR: {
        reconnectDelays: [0, 2000, 10000, 30000],
        logLevel: 'Information', // None, Critical, Error, Warning, Information, Debug, Trace
        timeoutInMilliseconds: 30000
    },
    
    // Chat Configuration
    chat: {
        maxMessageLength: 4000,
        typingIndicatorDelay: 1000,
        messageRetryAttempts: 3,
        defaultCollectionFallback: true
    },
    
    // Environment Detection
    environment: window.location.hostname === 'localhost' ? 'development' : 'production',
    
    // Debug Mode
    debug: window.location.hostname === 'localhost',
    
    // Endpoints
    endpoints: {
        auth: '/api/auth',
        conversations: '/api/conversations',
        collections: '/api/collections',
        documents: '/api/documents',
        chatToken: '/chat/token',
        signalRHub: '/hubs/chat'
    }
};

// Environment-specific overrides
if (window.appConfig.environment === 'development') {
    console.log('🔧 Development mode - App Config loaded:', window.appConfig);
}

// Export for modules that need it
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.appConfig;
}