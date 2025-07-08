/**
 * Chat Integrated Manager - Material Design 3 Integration
 * Handles SignalR communication and message sending for integrated chat interface
 */

class ChatManager {
    constructor() {
        this.chatConnection = null;
        this.isConnected = false;
        this.currentConversationId = null;
        this.messageQueue = [];
        this.settings = window.chatData?.settings || {};
        this.userId = window.chatData?.userId;
        this.companyId = window.chatData?.companyId;
        
        console.log('ChatManager initialized with:', {
            userId: this.userId,
            companyId: this.companyId,
            settings: this.settings
        });
    }

    async initialize() {
        console.log('Initializing ChatManager...');
        try {
            await this.initializeSignalR();
            this.bindGlobalEvents();
            console.log('ChatManager initialization complete');
        } catch (error) {
            console.error('Failed to initialize ChatManager:', error);
        }
    }

    async initializeSignalR() {
        try {
            console.log('Setting up SignalR connection...');

            // Get JWT token from server
            const token = await this.getJwtTokenFromCookie();

            // Build SignalR URL with token as query parameter
            const signalRUrl = token
                ? `http://localhost:5253/hubs/chat?access_token=${encodeURIComponent(token)}`
                : "http://localhost:5253/hubs/chat";

            this.chatConnection = new signalR.HubConnectionBuilder()
                .withUrl(signalRUrl, {
                    withCredentials: true,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupSignalRHandlers();

            // Start connection
            await this.chatConnection.start();
            this.isConnected = true;
            console.log('SignalR connection established successfully');

            // Process any queued messages
            this.processMessageQueue();

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.isConnected = false;
            throw error;
        }
    }

    async getJwtTokenFromCookie() {
        try {
            // Get JWT token from the server endpoint
            const response = await fetch('/chat/token', {
                method: 'GET',
                credentials: 'include', // Include cookies for authentication
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                return data.token;
            }
        } catch (error) {
            console.error('Error fetching JWT token:', error);
        }

        return null;
    }

    getSelectedCollections() {
        // Get selected collections from settings modal checkboxes
        const checkboxes = document.querySelectorAll('.collection-checkbox:checked');
        const selectedIds = Array.from(checkboxes).map(cb => cb.value);

        // If no collections selected, return all available collections as fallback
        if (selectedIds.length === 0 && this.settings.selectedCollectionIds) {
            return this.settings.selectedCollectionIds;
        }

        return selectedIds;
    }

    async loadConversationMessages(conversationId) {
        try {
            console.log(`Loading messages for conversation ${conversationId}`);

            const response = await fetch(`/api/conversations/${conversationId}/messages`, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const messages = await response.json();
                this.displayMessages(messages);
            } else {
                console.error('Failed to load conversation messages:', response.status);
            }
        } catch (error) {
            console.error('Error loading conversation messages:', error);
        }
    }

    displayMessages(messages) {
        const messagesList = document.getElementById('messages-list');
        if (!messagesList) return;

        messagesList.innerHTML = '';

        messages.forEach(message => {
            const messageElement = this.createMessageElement(message);
            messagesList.appendChild(messageElement);
        });

        // Scroll to bottom
        messagesList.scrollTop = messagesList.scrollHeight;
    }

    createMessageElement(message) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${message.isUser ? 'user-message' : 'assistant-message'} mb-3`;

        messageDiv.innerHTML = `
            <div class="d-flex ${message.isUser ? 'justify-content-end' : 'justify-content-start'}">
                <div class="message-bubble ${message.isUser ? 'bg-primary text-white' : 'bg-light'}" style="max-width: 70%; padding: 12px; border-radius: 12px;">
                    <div class="message-content">${this.formatMessageContent(message.content)}</div>
                    <small class="message-time text-muted d-block mt-1" style="font-size: 0.75rem;">
                        ${new Date(message.timestamp).toLocaleTimeString()}
                    </small>
                </div>
            </div>
        `;

        return messageDiv;
    }

    formatMessageContent(content) {
        // Basic markdown-like formatting
        return content
            .replace(/\n/g, '<br>')
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>');
    }

    setupSignalRHandlers() {
        console.log('Setting up SignalR handlers...');

        // Message received handler
        this.chatConnection.on("ReceiveMessage", (message) => {
            console.log('✅ SignalR ReceiveMessage event:', message);
            this.handleMessageReceived(message);
        });

        // Message chunk received (for streaming)
        this.chatConnection.on("MessageStreaming", (chunk) => {
            console.log('✅ SignalR MessageStreaming event:', chunk);
            this.handleMessageChunk(chunk);
        });

        // Conversation created handler
        this.chatConnection.on("ConversationCreated", (data) => {
            console.log('✅ SignalR ConversationCreated event:', data);
            this.handleConversationCreated(data);
        });

        // Conversation updated handler
        this.chatConnection.on("ConversationUpdated", (conversationId, updateType) => {
            console.log('✅ SignalR ConversationUpdated event:', conversationId, updateType);
        });

        // Error handler
        this.chatConnection.on("MessageError", (error) => {
            console.error('❌ SignalR MessageError event:', error);
            this.handleMessageError(error);
        });

        // Connection state handlers
        this.chatConnection.onreconnecting(() => {
            console.log('🔄 SignalR reconnecting...');
            this.isConnected = false;
        });

        this.chatConnection.onreconnected(() => {
            console.log('✅ SignalR reconnected');
            this.isConnected = true;
            this.processMessageQueue();

            // Rejoin current conversation if any
            if (this.currentConversationId) {
                this.chatConnection.invoke("JoinConversation", this.currentConversationId)
                    .catch(err => console.error('Error rejoining conversation after reconnect:', err));
            }
        });

        this.chatConnection.onclose((error) => {
            console.log('❌ SignalR connection closed', error);
            this.isConnected = false;
        });

        console.log('SignalR handlers setup complete');
    }

    bindGlobalEvents() {
        // Global event delegation for dynamically loaded content
        document.addEventListener('submit', (e) => {
            if (e.target.id === 'messageForm') {
                e.preventDefault();
                this.handleMessageSubmit();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.target.id === 'message-input' && e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.handleMessageSubmit();
            }
        });

        document.addEventListener('click', (e) => {
            if (e.target.id === 'send-button' || e.target.closest('#send-button')) {
                e.preventDefault();
                this.handleMessageSubmit();
            }
        });

        console.log('Global event handlers bound');
    }

    handleMessageSubmit() {
        const messageInput = document.getElementById('message-input');
        if (!messageInput) {
            console.error('Message input not found');
            return;
        }

        const content = messageInput.value.trim();
        if (!content) {
            console.log('Empty message, not sending');
            return;
        }

        console.log('Sending message:', content);
        this.sendMessage(content);
        messageInput.value = '';
        this.autoResizeTextarea(messageInput);
    }

    async sendMessage(content, conversationId = null) {
        if (!this.isConnected) {
            console.log('Not connected, queueing message');
            this.queueMessage(content, conversationId);
            return;
        }

        try {
            const targetConversationId = conversationId || this.currentConversationId;
            
            // Prepare message data
            const messageData = {
                content: content,
                conversationId: targetConversationId,
                collections: this.getSelectedCollections(),
                settings: this.settings,
                timestamp: new Date().toISOString()
            };

            console.log('Sending message data:', messageData);

            // Add user message to UI immediately
            this.addUserMessageToUI(content);

            // Send via SignalR
            if (targetConversationId) {
                await this.chatConnection.invoke("SendMessage", messageData);
            } else {
                // Create new conversation first
                console.log('Creating new conversation...');
                const newConversation = await this.createNewConversation();
                if (newConversation) {
                    messageData.conversationId = newConversation.conversationId;
                    this.currentConversationId = newConversation.conversationId;
                    await this.chatConnection.invoke("SendMessage", messageData);
                }
            }

        } catch (error) {
            console.error("Error sending message:", error);
            this.showErrorMessage("Failed to send message. Please try again.");
        }
    }

    addUserMessageToUI(content) {
        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) return;

        const messageId = 'temp_' + Date.now();
        const messageHtml = `
            <div class="message user" data-message-id="${messageId}">
                <div class="message-avatar">
                    <i class="material-symbols-rounded">person</i>
                </div>
                <div class="message-content">
                    <div class="message-text">${this.escapeHtml(content)}</div>
                    <div class="message-time">${new Date().toLocaleTimeString()}</div>
                </div>
            </div>
        `;

        messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
        this.scrollToBottom();
    }

    handleMessageReceived(message) {
        console.log('Handling received message:', message);

        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) {
            console.error('Messages container not found');
            return;
        }

        // Remove any temporary messages and typing indicator
        this.removeTempMessages();
        this.hideTypingIndicator();

        // Handle timestamp - use createdAt if timestamp is not available
        const timestamp = message.timestamp || message.createdAt || new Date().toISOString();

        // Build citations HTML if available
        let citationsHtml = '';
        if (message.citations && message.citations.length > 0) {
            citationsHtml = `
                <div class="message-citations">
                    <h6 class="citations-title">
                        <i class="material-symbols-rounded">format_quote</i> Sources
                    </h6>
                    <div class="citations-list">
                        ${message.citations.map(citation => `
                            <div class="citation-item">
                                <div class="citation-header">
                                    <span class="citation-document">${citation.documentName || 'Document'}</span>
                                    <span class="citation-score badge bg-info">${citation.score || '0.00'}</span>
                                </div>
                                <div class="citation-text">${citation.text || citation.content || ''}</div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        const messageHtml = `
            <div class="message assistant-message" data-message-id="${message.id}">
                <div class="message-avatar">
                    <i class="material-symbols-rounded">smart_toy</i>
                </div>
                <div class="message-content assistant-content">
                    <div class="message-text">${this.formatMessage(message.content)}</div>
                    ${citationsHtml}
                    <div class="message-meta">
                        <span class="message-time">${new Date(timestamp).toLocaleTimeString()}</span>
                    </div>
                </div>
            </div>
        `;

        messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
        this.scrollToBottom();

        console.log('Message displayed successfully');
    }

    handleMessageChunk(chunk) {
        // Handle streaming message chunks
        console.log('Handling message chunk:', chunk);

        if (chunk.isComplete) {
            // Streaming is complete
            console.log('Message streaming completed');
            this.hideTypingIndicator();
            return;
        }

        // Find or create streaming message container
        let streamingMessage = document.getElementById('streaming-message');
        if (!streamingMessage) {
            const messagesList = document.getElementById('messages-list');
            if (messagesList) {
                const messageElement = this.createStreamingMessageElement(chunk);
                messagesList.appendChild(messageElement);
                streamingMessage = messageElement.querySelector('#streaming-message');
            }
        }

        // Append chunk content
        if (streamingMessage && chunk.content) {
            streamingMessage.innerHTML += chunk.content;
            this.scrollToBottom();
        }
    }

    createStreamingMessageElement(chunk) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message assistant-message mb-3';

        messageDiv.innerHTML = `
            <div class="d-flex justify-content-start">
                <div class="message-bubble bg-light" style="max-width: 70%; padding: 12px; border-radius: 12px;">
                    <div id="streaming-message" class="message-content"></div>
                    <small class="message-time text-muted d-block mt-1" style="font-size: 0.75rem;">
                        ${new Date().toLocaleTimeString()}
                    </small>
                </div>
            </div>
        `;

        return messageDiv;
    }

    handleConversationCreated(data) {
        console.log('🎉 Conversation created:', data);

        // Update current conversation ID
        this.currentConversationId = data.conversationId;

        // Update UI to show conversation is active
        const chatHeader = document.querySelector('.chat-header h6');
        if (chatHeader) {
            chatHeader.textContent = data.title || 'New Conversation';
        }

        // ✅ CRITICAL FIX: Join the conversation room for real-time updates
        if (this.chatConnection && this.isConnected) {
            console.log('🔥 Joining conversation group:', data.conversationId);
            this.chatConnection.invoke("JoinConversation", data.conversationId)
                .then(() => {
                    console.log('✅ Successfully joined conversation group:', data.conversationId);
                })
                .catch(err => {
                    console.error('❌ Error joining conversation:', err);
                });
        } else {
            console.error('❌ Cannot join conversation - SignalR not connected');
        }
    }

    handleMessageError(error) {
        console.error('Message error received:', error);

        // Show error message to user
        const messagesList = document.getElementById('messages-list');
        if (messagesList) {
            const errorElement = this.createErrorMessageElement(error);
            messagesList.appendChild(errorElement);
            this.scrollToBottom();
        }

        // Re-enable message input
        const messageInput = document.getElementById('message-input');
        const sendButton = document.getElementById('send-button');
        if (messageInput) messageInput.disabled = false;
        if (sendButton) sendButton.disabled = false;
    }

    createErrorMessageElement(error) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message error-message mb-3';

        messageDiv.innerHTML = `
            <div class="d-flex justify-content-center">
                <div class="alert alert-danger" style="max-width: 70%;">
                    <i class="material-symbols-rounded me-2">error</i>
                    ${error}
                </div>
            </div>
        `;

        return messageDiv;
    }

    showTypingIndicator() {
        // Add typing indicator if not already present
        let typingIndicator = document.getElementById('typing-indicator');
        if (!typingIndicator) {
            const messagesList = document.getElementById('messages-list');
            if (messagesList) {
                const typingElement = document.createElement('div');
                typingElement.id = 'typing-indicator';
                typingElement.className = 'message assistant-message mb-3';
                typingElement.innerHTML = `
                    <div class="d-flex justify-content-start">
                        <div class="message-bubble bg-light" style="max-width: 70%; padding: 12px; border-radius: 12px;">
                            <div class="typing-dots">
                                <span class="typing-dot"></span>
                                <span class="typing-dot"></span>
                                <span class="typing-dot"></span>
                            </div>
                        </div>
                    </div>
                `;
                messagesList.appendChild(typingElement);
                this.scrollToBottom();
            }
        }
    }

    hideTypingIndicator() {
        const typingIndicator = document.getElementById('typing-indicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    displayUserMessage(content) {
        const messagesList = document.getElementById('messages-list');
        if (!messagesList) return;

        const userMessage = {
            content: content,
            isUser: true,
            timestamp: new Date().toISOString()
        };

        const messageElement = this.createMessageElement(userMessage);
        messagesList.appendChild(messageElement);
        this.scrollToBottom();
    }

    scrollToBottom() {
        const messagesList = document.getElementById('messages-list');
        if (messagesList) {
            messagesList.scrollTop = messagesList.scrollHeight;
        }
    }

    queueMessage(content, conversationId) {
        this.messageQueue.push({ content, conversationId });
        console.log('Message queued:', { content, conversationId });
    }

    processMessageQueue() {
        while (this.messageQueue.length > 0 && this.isConnected) {
            const message = this.messageQueue.shift();
            this.sendMessage(message.content, message.conversationId);
        }
    }

    updateSelectedCollectionsDisplay() {
        const selectedCollections = this.getSelectedCollections();
        const display = document.getElementById('selectedCollections');
        if (display) {
            display.textContent = `${selectedCollections.length} collections selected`;
        }
    }

    async createNewConversation() {
        try {
            const response = await fetch('/chat/createconversation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    title: 'New Conversation',
                    collections: this.getSelectedCollections()
                })
            });

            if (response.ok) {
                const result = await response.json();
                console.log('New conversation created:', result);
                return result;
            } else {
                throw new Error('Failed to create conversation');
            }
        } catch (error) {
            console.error('Error creating conversation:', error);
            return null;
        }
    }

    removeTempMessages() {
        const tempMessages = document.querySelectorAll('.message[data-message-id^="temp_"]');
        tempMessages.forEach(msg => msg.remove());
    }

    autoResizeTextarea(textarea) {
        if (!textarea) return;
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
    }

    scrollToBottom() {
        const container = document.getElementById('messagesContainer');
        if (container) {
            container.scrollTop = container.scrollHeight;
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    formatMessage(content) {
        // Basic markdown-like formatting
        return content
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');
    }

    showErrorMessage(message) {
        // Show error message to user
        console.error(message);
        // Could implement toast notification here
    }
}

// Global functions for UI events (called from HTML)
async function newConversation() {
    if (window.chatManager) {
        console.log('Starting new conversation...');

        // Reset current conversation
        window.chatManager.currentConversationId = null;

        // Show the conversation container and hide the welcome screen
        const welcomeScreen = document.querySelector('.welcome-screen');
        const conversationContainer = document.getElementById('conversation-container');

        if (welcomeScreen) welcomeScreen.style.display = 'none';
        if (conversationContainer) {
            conversationContainer.style.display = 'block';
            // Load the chat interface
            await loadChatInterface();
        }
    }
}

async function loadChatInterface(conversationId = null) {
    const conversationContainer = document.getElementById('conversation-container');
    if (!conversationContainer) return;

    // Create the chat interface HTML
    const chatHTML = `
        <div class="d-flex flex-column h-100">
            <!-- Chat Header -->
            <div class="chat-header border-bottom p-3 bg-light">
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <h6 class="mb-0">${conversationId ? 'Conversation' : 'New Conversation'}</h6>
                        <small class="text-muted">Chat with your documents</small>
                    </div>
                    <div>
                        <button class="btn btn-sm btn-outline-secondary me-2" onclick="showSettings()">
                            <i class="material-symbols-rounded">settings</i>
                        </button>
                        <button class="btn btn-sm btn-outline-secondary" onclick="backToConversations()">
                            <i class="material-symbols-rounded">arrow_back</i>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Messages Area -->
            <div class="messages-container flex-grow-1 p-3" style="overflow-y: auto; max-height: calc(100vh - 200px);">
                <div id="messagesContainer">
                    <!-- Messages will be loaded here -->
                </div>
            </div>

            <!-- Message Input -->
            <div class="message-input-container border-top p-3 bg-light">
                <div class="row g-2">
                    <div class="col">
                        <div class="input-group">
                            <textarea id="message-input" class="form-control" placeholder="Type your message..." rows="1" style="resize: none;"></textarea>
                            <button class="btn btn-primary" onclick="sendMessage()" id="send-button">
                                <i class="material-symbols-rounded">send</i>
                            </button>
                        </div>
                    </div>
                </div>
                <div class="mt-2">
                    <small class="text-muted">Press Enter to send, Shift+Enter for new line</small>
                </div>
            </div>
        </div>
    `;

    conversationContainer.innerHTML = chatHTML;

    // Set up message input event handlers
    const messageInput = document.getElementById('message-input');
    if (messageInput) {
        messageInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // Auto-resize textarea
        messageInput.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 120) + 'px';
        });

        // Focus on the input
        messageInput.focus();
    }

    // Load existing messages if conversationId is provided
    if (conversationId && window.chatManager) {
        await window.chatManager.loadConversationMessages(conversationId);
    }
}

function backToConversations() {
    const welcomeScreen = document.querySelector('.welcome-screen');
    const conversationContainer = document.getElementById('conversation-container');

    if (welcomeScreen) welcomeScreen.style.display = 'block';
    if (conversationContainer) conversationContainer.style.display = 'none';

    // Reset current conversation
    if (window.chatManager) {
        window.chatManager.currentConversationId = null;
    }
}

async function sendMessage() {
    const messageInput = document.getElementById('message-input');
    const sendButton = document.getElementById('send-button');

    if (!messageInput || !window.chatManager) return;

    const message = messageInput.value.trim();
    if (!message) return;

    // Disable input and button
    messageInput.disabled = true;
    sendButton.disabled = true;

    try {
        // Display user message immediately
        window.chatManager.displayUserMessage(message);

        // Clear input
        messageInput.value = '';
        messageInput.style.height = 'auto';

        // Show typing indicator
        window.chatManager.showTypingIndicator();

        // Send message via ChatManager with correct parameters
        await window.chatManager.sendMessage(message, window.chatManager.currentConversationId);

    } catch (error) {
        console.error('Error sending message:', error);
        window.chatManager.hideTypingIndicator();
        window.chatManager.handleMessageError('Failed to send message. Please try again.');
    } finally {
        // Re-enable input and button
        messageInput.disabled = false;
        sendButton.disabled = false;
        messageInput.focus();
    }
}

function loadConversation(conversationId) {
    if (window.chatManager) {
        window.chatManager.currentConversationId = conversationId;

        // Join the conversation room for real-time updates
        if (window.chatManager.chatConnection && window.chatManager.isConnected) {
            window.chatManager.chatConnection.invoke("JoinConversation", conversationId)
                .then(() => {
                    console.log(`Joined conversation ${conversationId} for real-time updates`);
                })
                .catch(err => console.error('Error joining conversation:', err));
        }

        // Load conversation via AJAX
        fetch(`/chat/loadconversation/${conversationId}`)
            .then(response => response.text())
            .then(html => {
                const container = document.getElementById('conversation-container');
                const placeholder = document.getElementById('chat-placeholder');
                if (container && placeholder) {
                    container.innerHTML = html;
                    container.style.display = 'block';
                    placeholder.style.display = 'none';
                }
            })
            .catch(error => console.error('Error loading conversation:', error));
    }
}

// Global functions for UI events (called from HTML)

function showSettings() {
    console.log('Opening chat settings modal...');
    const modal = new bootstrap.Modal(document.getElementById('chatSettingsModal'));
    modal.show();

    // Update collections display when modal opens
    if (window.chatManager) {
        window.chatManager.updateSelectedCollectionsDisplay();
    }
}

function saveSettings() {
    console.log('Saving chat settings...');

    try {
        // Get selected collections
        const selectedCollections = [];
        const checkboxes = document.querySelectorAll('.collection-checkbox:checked');
        checkboxes.forEach(cb => selectedCollections.push(cb.value));

        // Get other settings
        const settings = {
            selectedCollectionIds: selectedCollections,
            relevanceThreshold: parseFloat(document.getElementById('relevanceThreshold').value),
            maxResults: parseInt(document.getElementById('maxResults').value),
            searchMode: document.getElementById('searchMode').value,
            useVectorSearch: document.getElementById('useVectorSearch').checked,
            useHybridSearch: document.getElementById('useHybridSearch').checked,
            includeTitleIfAvailable: document.getElementById('includeTitleIfAvailable').checked
        };

        // Update ChatManager settings
        if (window.chatManager) {
            window.chatManager.settings = { ...window.chatManager.settings, ...settings };
            window.chatManager.updateSelectedCollectionsDisplay();
        }

        // Save to server
        fetch('/Chat/SaveSettings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(settings)
        })
        .then(response => {
            if (response.ok) {
                console.log('Settings saved successfully');
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('chatSettingsModal'));
                modal.hide();

                // Show success message
                showToast('Settings saved successfully', 'success');
            } else {
                throw new Error('Failed to save settings');
            }
        })
        .catch(error => {
            console.error('Error saving settings:', error);
            showToast('Failed to save settings', 'error');
        });

    } catch (error) {
        console.error('Error in saveSettings:', error);
        showToast('Failed to save settings', 'error');
    }
}

function deleteConversation(conversationId) {
    if (!conversationId) return;

    if (confirm('Are you sure you want to delete this conversation? This action cannot be undone.')) {
        fetch(`/chat/deleteconversation/${conversationId}`, {
            method: 'DELETE',
            headers: {
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (response.ok) {
                console.log('Conversation deleted successfully');
                // Reload conversations list
                location.reload();
            } else {
                throw new Error('Failed to delete conversation');
            }
        })
        .catch(error => {
            console.error('Error deleting conversation:', error);
            showToast('Failed to delete conversation', 'error');
        });
    }
}

function exportConversation() {
    console.log('Opening export modal...');
    const modal = new bootstrap.Modal(document.getElementById('exportModal'));
    modal.show();
}

function performExport() {
    const format = document.getElementById('exportFormat').value;
    const includeCitations = document.getElementById('includeCitations').checked;
    const conversationId = window.chatManager?.currentConversationId;

    if (!conversationId) {
        showToast('No conversation selected for export', 'error');
        return;
    }

    const exportUrl = `/chat/export/${conversationId}?format=${format}&includeCitations=${includeCitations}`;
    window.open(exportUrl, '_blank');

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('exportModal'));
    modal.hide();
}

function backToConversations() {
    console.log('Returning to conversations list...');

    // Hide conversation container and show placeholder
    const container = document.getElementById('conversation-container');
    const placeholder = document.getElementById('chat-placeholder');

    if (container && placeholder) {
        container.style.display = 'none';
        placeholder.style.display = 'flex';
    }

    // Clear current conversation
    if (window.chatManager) {
        window.chatManager.currentConversationId = null;
    }

    // Remove active state from conversation items
    document.querySelectorAll('.conversation-item').forEach(item => {
        item.classList.remove('active');
    });
}

function filterConversations(searchTerm) {
    const conversations = document.querySelectorAll('.conversation-item');
    const term = searchTerm.toLowerCase();

    conversations.forEach(conv => {
        const title = conv.querySelector('h6').textContent.toLowerCase();
        const lastMessage = conv.querySelector('.text-muted').textContent.toLowerCase();

        if (title.includes(term) || lastMessage.includes(term)) {
            conv.style.display = 'block';
        } else {
            conv.style.display = 'none';
        }
    });
}

function showToast(message, type = 'info') {
    // Simple toast notification
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
    toast.style.zIndex = '9999';
    toast.textContent = message;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}

// Export for global access
window.ChatManager = ChatManager;
