class ChatManager {
    constructor() {
        this.currentConversationId = null;
        this.isConnected = false;
        this.messageQueue = [];
        this.settings = this.loadSettings();
        this.isTyping = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        
        this.initializeSignalR();
        this.bindEvents();
    }
    
    // SignalR Integration con Hubs esistenti
    async initializeSignalR() {
        try {
            // Usa CollectionHub esistente per notifiche collections
            this.collectionConnection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/collection")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();
                
            // Crea nuovo ChatHub per messaging real-time
            this.chatConnection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/chat")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();
                
            // Collection Hub Event handlers
            this.collectionConnection.on("CollectionUpdated", (collection, updateType) => {
                this.handleCollectionUpdate(collection, updateType);
            });
            
            // Chat Hub Event handlers
            this.chatConnection.on("ReceiveMessage", (message) => {
                this.handleIncomingMessage(message);
            });
            
            this.chatConnection.on("MessageStreaming", (chunk) => {
                this.handleStreamingChunk(chunk);
            });
            
            this.chatConnection.on("ConversationUpdated", (conversationId, updateType) => {
                this.refreshConversation(conversationId, updateType);
            });
            
            this.chatConnection.on("TypingIndicator", (conversationId, userId, isTyping) => {
                this.handleTypingIndicator(conversationId, userId, isTyping);
            });
            
            // Connection state handlers
            this.chatConnection.onreconnecting(() => {
                console.log("Chat connection lost, attempting to reconnect...");
                this.showConnectionStatus("Riconnessione in corso...", "warning");
            });
            
            this.chatConnection.onreconnected(() => {
                console.log("Chat connection reestablished");
                this.showConnectionStatus("Connesso", "success");
                this.reconnectAttempts = 0;
            });
            
            this.chatConnection.onclose(() => {
                console.log("Chat connection closed");
                this.isConnected = false;
                this.showConnectionStatus("Disconnesso", "danger");
                this.handleConnectionError();
            });
            
            // Start connections
            await Promise.all([
                this.chatConnection.start(),
                this.collectionConnection.start()
            ]);
            
            this.isConnected = true;
            console.log("SignalR connected successfully");
            this.showConnectionStatus("Connesso", "success");
            
            // Join conversation room if we have one
            if (this.currentConversationId) {
                await this.joinConversationRoom(this.currentConversationId);
            }
            
            // Process queued messages
            this.processMessageQueue();
            
        } catch (err) {
            console.error("SignalR connection failed:", err);
            this.handleConnectionError(err);
        }
    }
    
    // Message sending con streaming support
    async sendMessage(content, conversationId = null) {
        if (!this.isConnected) {
            this.queueMessage(content, conversationId);
            return;
        }
        
        const targetConversationId = conversationId || this.currentConversationId;
        
        const messageData = {
            content: content,
            conversationId: targetConversationId,
            collections: this.getSelectedCollections(),
            settings: this.settings,
            timestamp: new Date().toISOString()
        };
        
        try {
            // Show typing indicator
            this.showTypingIndicator();
            
            // Add user message to UI immediately
            const tempMessageId = this.generateTempId();
            this.addMessageToUI({
                id: tempMessageId,
                content: content,
                role: "user",
                createdAt: new Date(),
                isTemporary: true
            });
            
            // Clear input
            const messageInput = document.getElementById('messageInput');
            if (messageInput) {
                messageInput.value = '';
                this.autoResizeTextarea(messageInput);
            }
            
            // Send via SignalR for streaming response
            if (targetConversationId) {
                await this.chatConnection.invoke("SendMessage", messageData);
            } else {
                // Create new conversation first
                const newConversation = await this.createNewConversation();
                if (newConversation) {
                    messageData.conversationId = newConversation.conversationId;
                    this.currentConversationId = newConversation.conversationId;
                    await this.chatConnection.invoke("SendMessage", messageData);
                }
            }
            
        } catch (error) {
            console.error("Error sending message:", error);
            this.hideTypingIndicator();
            this.handleSendError(error);
        }
    }
    
    // Streaming message handling
    handleStreamingChunk(chunk) {
        const { messageId, content, isComplete, citations, error } = chunk;
        
        let messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
        
        if (!messageElement) {
            // Create new message element for streaming
            messageElement = this.createStreamingMessage(messageId);
        }
        
        if (error) {
            this.handleStreamingError(messageElement, error);
            return;
        }
        
        // Update content progressively
        const contentElement = messageElement.querySelector('.message-text');
        if (isComplete) {
            contentElement.innerHTML = this.formatMessage(content);
            this.hideTypingIndicator();
            
            // Add citations if present
            if (citations && citations.length > 0) {
                this.addCitationsToMessage(messageElement, citations);
            }
            
            // Mark as complete
            messageElement.classList.remove('streaming');
            
            // Scroll to bottom
            this.scrollToBottom();
        } else {
            // Progressive content update
            contentElement.innerHTML = this.formatMessage(content) + '<span class="cursor">|</span>';
            this.scrollToBottom();
        }
    }
    
    // Create streaming message element
    createStreamingMessage(messageId) {
        const container = document.getElementById('messagesContainer');
        const messageElement = this.createMessageElement({
            id: messageId,
            content: '',
            role: 'assistant',
            createdAt: new Date(),
            isStreaming: true
        });
        
        messageElement.classList.add('streaming');
        container.appendChild(messageElement);
        return messageElement;
    }
    
    // Handle incoming complete messages
    handleIncomingMessage(message) {
        // Remove any temporary messages
        this.removeTempMessages();
        
        // Add the complete message
        this.addMessageToUI(message);
        
        // Hide typing indicator
        this.hideTypingIndicator();
        
        // Update conversation list
        this.updateConversationInList(message.conversationId);
    }
    
    // Conversation branching
    async branchConversation(messageId) {
        try {
            const response = await fetch(`/Chat/Branch`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ 
                    conversationId: this.currentConversationId,
                    fromMessageId: messageId 
                })
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    // Navigate to new branch
                    window.location.href = `/Chat/Conversation/${result.data.conversationId}`;
                }
            } else {
                throw new Error('Failed to create branch');
            }
        } catch (error) {
            console.error("Error branching conversation:", error);
            this.showError("Errore durante la creazione del branch");
        }
    }
    
    // Export functionality
    async exportConversation(format = 'json', includeCitations = true) {
        try {
            const response = await fetch(`/Chat/Export/${this.currentConversationId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ 
                    format: format,
                    includeCitations: includeCitations 
                })
            });
            
            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `conversation_${this.currentConversationId}.${format}`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                
                this.showSuccess("Conversazione esportata con successo");
            } else {
                throw new Error('Export failed');
            }
        } catch (error) {
            console.error("Error exporting conversation:", error);
            this.showError("Errore durante l'export");
        }
    }
    
    // New conversation
    async newConversation() {
        try {
            const selectedCollections = this.getSelectedCollections();
            const response = await this.createNewConversation(selectedCollections);
            
            if (response && response.conversationId) {
                // Navigate to new conversation
                window.location.href = `/Chat/Conversation/${response.conversationId}`;
            }
        } catch (error) {
            console.error("Error creating new conversation:", error);
            this.showError("Errore durante la creazione della conversazione");
        }
    }
    
    async createNewConversation(collections = []) {
        const response = await fetch('/Chat/CreateConversation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': this.getAntiForgeryToken()
            },
            body: JSON.stringify({
                title: 'Nuova Conversazione',
                collections: collections
            })
        });
        
        if (response.ok) {
            const result = await response.json();
            return result.success ? result : null;
        }
        
        return null;
    }
    
    // Settings management
    saveSettings() {
        const settings = {
            selectedCollections: this.getSelectedCollections(),
            relevanceThreshold: parseFloat(document.getElementById('relevanceThreshold')?.value || 0.7),
            maxResults: parseInt(document.getElementById('maxResults')?.value || 10),
            searchMode: document.getElementById('searchMode')?.value || 'hybrid',
            useVectorSearch: document.getElementById('useVectorSearch')?.checked || true,
            useHybridSearch: document.getElementById('useHybridSearch')?.checked || true,
            includeTitleIfAvailable: document.getElementById('includeTitleIfAvailable')?.checked || true
        };
        
        localStorage.setItem('chatSettings', JSON.stringify(settings));
        this.settings = settings;
        
        // Update threshold display
        const thresholdDisplay = document.getElementById('thresholdValue');
        if (thresholdDisplay) {
            thresholdDisplay.textContent = settings.relevanceThreshold;
        }
    }
    
    loadSettings() {
        const saved = localStorage.getItem('chatSettings');
        return saved ? JSON.parse(saved) : {
            selectedCollections: [],
            relevanceThreshold: 0.7,
            maxResults: 10,
            searchMode: 'hybrid',
            useVectorSearch: true,
            useHybridSearch: true,
            includeTitleIfAvailable: true
        };
    }
    
    getSelectedCollections() {
        const checkboxes = document.querySelectorAll('.collection-checkbox:checked');
        return Array.from(checkboxes).map(cb => cb.value);
    }
    
    // UI Helper methods
    addMessageToUI(message) {
        const container = document.getElementById('messagesContainer');
        if (!container) return;
        
        const messageElement = this.createMessageElement(message);
        container.appendChild(messageElement);
        this.scrollToBottom();
    }
    
    createMessageElement(message) {
        const div = document.createElement('div');
        div.className = `message ${message.role === 'user' ? 'user-message' : 'assistant-message'}`;
        div.setAttribute('data-message-id', message.id);
        
        const isUser = message.role === 'user';
        const timeStr = this.formatTime(message.createdAt);
        
        div.innerHTML = `
            ${isUser ? 
                `<div class="message-content user-content">
                    <div class="message-text">${this.formatMessage(message.content)}</div>
                    <div class="message-meta">
                        <span class="message-time">${timeStr}</span>
                    </div>
                </div>
                <div class="message-avatar"><i class="fas fa-user"></i></div>` :
                `<div class="message-avatar"><i class="fas fa-robot"></i></div>
                <div class="message-content assistant-content">
                    <div class="message-text">${this.formatMessage(message.content)}</div>
                    <div class="message-meta">
                        <span class="message-time">${timeStr}</span>
                        <div class="message-actions">
                            <button class="btn btn-sm btn-link" onclick="chatManager.copyMessage('${message.id}')" title="Copia">
                                <i class="fas fa-copy"></i>
                            </button>
                            <button class="btn btn-sm btn-link" onclick="chatManager.branchConversation('${message.id}')" title="Branch">
                                <i class="fas fa-code-branch"></i>
                            </button>
                        </div>
                    </div>
                </div>`
            }
        `;
        
        return div;
    }
    
    formatMessage(content) {
        // Basic markdown-like formatting
        return content
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/`(.*?)`/g, '<code>$1</code>')
            .replace(/\n/g, '<br>');
    }
    
    formatTime(date) {
        const d = new Date(date);
        return d.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });
    }
    
    generateTempId() {
        return 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }
    
    scrollToBottom() {
        const container = document.getElementById('messagesContainer');
        if (container) {
            container.scrollTop = container.scrollHeight;
        }
    }
    
    autoResizeTextarea(textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
    }
    
    // Typing indicators
    showTypingIndicator() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) {
            indicator.style.display = 'block';
            this.scrollToBottom();
        }
    }
    
    hideTypingIndicator() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) {
            indicator.style.display = 'none';
        }
    }
    
    // Connection status
    showConnectionStatus(message, type) {
        // Implementation for connection status display
        console.log(`Connection status: ${message} (${type})`);
    }
    
    // Error handling
    handleConnectionError(error) {
        this.isConnected = false;
        this.reconnectAttempts++;
        
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            setTimeout(() => {
                this.initializeSignalR();
            }, Math.pow(2, this.reconnectAttempts) * 1000);
        } else {
            this.showError("Impossibile connettersi al server. Ricarica la pagina.");
        }
    }
    
    handleSendError(error) {
        this.showError("Errore durante l'invio del messaggio. Riprova.");
        this.hideTypingIndicator();
    }
    
    showError(message) {
        this.showToast(message, 'danger');
    }
    
    showSuccess(message) {
        this.showToast(message, 'success');
    }
    
    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.textContent = message;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 5000);
    }
    
    // Utility methods
    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }
    
    queueMessage(content, conversationId) {
        this.messageQueue.push({ content, conversationId });
    }
    
    processMessageQueue() {
        while (this.messageQueue.length > 0 && this.isConnected) {
            const message = this.messageQueue.shift();
            this.sendMessage(message.content, message.conversationId);
        }
    }
    
    removeTempMessages() {
        const tempMessages = document.querySelectorAll('.message[data-message-id^="temp_"]');
        tempMessages.forEach(msg => msg.remove());
    }

    // Event binding
    bindEvents() {
        // Message form
        const messageForm = document.getElementById('messageForm');
        if (messageForm) {
            messageForm.addEventListener('submit', (e) => {
                e.preventDefault();
                const input = document.getElementById('messageInput');
                if (input && input.value.trim()) {
                    this.sendMessage(input.value.trim());
                }
            });
        }

        // Auto-resize textarea
        const messageInput = document.getElementById('messageInput');
        if (messageInput) {
            messageInput.addEventListener('input', (e) => {
                this.autoResizeTextarea(e.target);

                // Update character count
                const charCount = document.getElementById('characterCount');
                if (charCount) {
                    charCount.textContent = e.target.value.length;
                }
            });
        }

        // Settings changes
        document.addEventListener('change', (e) => {
            if (e.target.matches('.collection-checkbox, #relevanceThreshold, #maxResults, #searchMode, #useVectorSearch, #useHybridSearch, #includeTitleIfAvailable')) {
                this.saveSettings();
            }
        });

        // Threshold slider update
        const thresholdSlider = document.getElementById('relevanceThreshold');
        if (thresholdSlider) {
            thresholdSlider.addEventListener('input', (e) => {
                const display = document.getElementById('thresholdValue');
                if (display) {
                    display.textContent = e.target.value;
                }
            });
        }

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey || e.metaKey) {
                switch(e.key) {
                    case 'Enter':
                        if (document.activeElement === messageInput) {
                            e.preventDefault();
                            messageForm?.dispatchEvent(new Event('submit'));
                        }
                        break;
                    case 'n':
                        e.preventDefault();
                        this.newConversation();
                        break;
                    case 'e':
                        e.preventDefault();
                        this.exportConversation();
                        break;
                }
            }
        });

        // Search conversations
        const conversationSearch = document.getElementById('conversationSearch');
        if (conversationSearch) {
            conversationSearch.addEventListener('input', (e) => {
                this.filterConversations(e.target.value);
            });
        }
    }

    // Additional helper methods
    async joinConversationRoom(conversationId) {
        if (this.chatConnection && this.isConnected) {
            try {
                await this.chatConnection.invoke("JoinConversation", conversationId);
            } catch (error) {
                console.error("Error joining conversation room:", error);
            }
        }
    }

    async leaveConversationRoom(conversationId) {
        if (this.chatConnection && this.isConnected) {
            try {
                await this.chatConnection.invoke("LeaveConversation", conversationId);
            } catch (error) {
                console.error("Error leaving conversation room:", error);
            }
        }
    }

    handleCollectionUpdate(collection, updateType) {
        // Update collection in UI
        console.log("Collection updated:", collection, updateType);
        // Refresh collections list if needed
    }

    refreshConversation(conversationId, updateType) {
        if (conversationId === this.currentConversationId) {
            // Refresh current conversation
            console.log("Current conversation updated:", updateType);
        }

        // Update conversation in sidebar
        this.updateConversationInList(conversationId);
    }

    updateConversationInList(conversationId) {
        // Update conversation item in sidebar
        const convItem = document.querySelector(`[data-conversation-id="${conversationId}"]`);
        if (convItem) {
            // Update last message, timestamp, etc.
            console.log("Updating conversation in list:", conversationId);
        }
    }

    handleTypingIndicator(conversationId, userId, isTyping) {
        if (conversationId === this.currentConversationId) {
            // Show/hide typing indicator for other users
            console.log("Typing indicator:", userId, isTyping);
        }
    }

    handleStreamingError(messageElement, error) {
        const contentElement = messageElement.querySelector('.message-text');
        if (contentElement) {
            contentElement.innerHTML = `<span class="text-danger"><i class="fas fa-exclamation-triangle"></i> Errore: ${error}</span>`;
        }
        this.hideTypingIndicator();
    }

    addCitationsToMessage(messageElement, citations) {
        const citationsHtml = `
            <div class="message-citations">
                <h6 class="citations-title">
                    <i class="fas fa-quote-left"></i> Fonti
                </h6>
                <div class="citations-list">
                    ${citations.map(citation => `
                        <div class="citation-item" onclick="openDocument('${citation.documentId}', ${citation.pageNumber})">
                            <div class="citation-header">
                                <span class="citation-document">${citation.documentName}</span>
                                <span class="citation-score badge bg-info">${citation.score.toFixed(2)}</span>
                            </div>
                            <div class="citation-text">${citation.text}</div>
                            <div class="citation-meta">
                                <small class="text-muted">
                                    ${citation.pageNumber > 0 ? `Pagina ${citation.pageNumber}` : ''}
                                    ${citation.collectionName ? `• ${citation.collectionName}` : ''}
                                </small>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;

        const messageContent = messageElement.querySelector('.message-content');
        if (messageContent) {
            messageContent.insertAdjacentHTML('beforeend', citationsHtml);
        }
    }

    copyMessage(messageId) {
        const messageElement = document.querySelector(`[data-message-id="${messageId}"] .message-text`);
        if (messageElement) {
            navigator.clipboard.writeText(messageElement.textContent).then(() => {
                this.showSuccess('Messaggio copiato negli appunti');
            }).catch(() => {
                this.showError('Errore durante la copia');
            });
        }
    }

    filterConversations(query) {
        const conversations = document.querySelectorAll('.conversation-item');
        const lowerQuery = query.toLowerCase();

        conversations.forEach(conv => {
            const title = conv.querySelector('.conv-title')?.textContent.toLowerCase() || '';
            const preview = conv.querySelector('.conv-preview')?.textContent.toLowerCase() || '';

            if (title.includes(lowerQuery) || preview.includes(lowerQuery)) {
                conv.style.display = 'block';
            } else {
                conv.style.display = 'none';
            }
        });
    }
}

// Initialize chat manager
let chatManager;
document.addEventListener('DOMContentLoaded', () => {
    chatManager = new ChatManager();
});

// Global functions for UI events
function newConversation() {
    if (chatManager) {
        chatManager.newConversation();
    }
}

function loadConversation(id) {
    window.location.href = `/Chat/Conversation/${id}`;
}

function exportConversation() {
    const modal = new bootstrap.Modal(document.getElementById('exportModal'));
    modal.show();
}

function performExport() {
    const format = document.getElementById('exportFormat')?.value || 'json';
    const includeCitations = document.getElementById('includeCitations')?.checked || true;

    if (chatManager) {
        chatManager.exportConversation(format, includeCitations);
    }

    const modal = bootstrap.Modal.getInstance(document.getElementById('exportModal'));
    if (modal) {
        modal.hide();
    }
}

function deleteConversation(conversationId) {
    if (confirm('Sei sicuro di voler eliminare questa conversazione?')) {
        // Implement delete functionality
        console.log('Deleting conversation:', conversationId);
    }
}

function toggleSettings() {
    const settings = document.querySelector('.chat-settings');
    const toggle = document.getElementById('settingsToggle');

    if (settings) {
        settings.classList.toggle('collapsed');

        if (toggle) {
            const icon = toggle.querySelector('i');
            if (icon) {
                icon.className = settings.classList.contains('collapsed') ?
                    'fas fa-chevron-right' : 'fas fa-chevron-left';
            }
        }
    }
}

function filterConversations(query) {
    if (chatManager) {
        chatManager.filterConversations(query);
    }
}

function loadConversationPage(page) {
    // Implement pagination
    console.log('Loading conversation page:', page);
}

function openDocument(documentId, pageNumber) {
    // Open document in new tab/modal
    window.open(`/Documents/${documentId}?page=${pageNumber}`, '_blank');
}

function insertQuickMessage(text) {
    const input = document.getElementById('messageInput');
    if (input) {
        input.value = text;
        input.focus();

        if (chatManager) {
            chatManager.autoResizeTextarea(input);
        }

        // Position cursor at end
        input.setSelectionRange(input.value.length, input.value.length);
    }
}
