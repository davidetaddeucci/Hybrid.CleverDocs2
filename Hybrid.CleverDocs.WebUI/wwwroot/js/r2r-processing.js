/**
 * R2R Processing Status Manager
 * Gestisce la visualizzazione in tempo reale dello stato di elaborazione R2R
 */
class R2RProcessingManager {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.processingItems = new Map();
        this.statusContainer = null;
        this.globalStatusIndicator = null;
        
        // R2R Status configurations with UI settings
        this.statusConfig = {
            'Queued': {
                icon: 'schedule',
                color: 'secondary',
                animation: 'pulse',
                message: 'Queued for processing'
            },
            'Processing': {
                icon: 'sync',
                color: 'warning',
                animation: 'spin',
                message: 'Processing in progress'
            },
            'Chunking': {
                icon: 'auto_awesome_motion',
                color: 'info',
                animation: 'pulse',
                message: 'Splitting into chunks'
            },
            'R2RIngestion': {
                icon: 'cloud_upload',
                color: 'primary',
                animation: 'bounce',
                message: 'Sending to R2R API'
            },
            'Indexing': {
                icon: 'search',
                color: 'info',
                animation: 'pulse',
                message: 'Indexing'
            },
            'Completed': {
                icon: 'check_circle',
                color: 'success',
                animation: 'none',
                message: 'Completed successfully'
            },
            'Failed': {
                icon: 'error',
                color: 'danger',
                animation: 'shake',
                message: 'Processing failed'
            },
            'Retrying': {
                icon: 'refresh',
                color: 'warning',
                animation: 'spin',
                message: 'Retrying in progress'
            }
        };
        
        this.init();
    }
    
    async init() {
        try {
            await this.initializeSignalR();
            this.setupUI();
            this.bindEvents();
            console.log('R2R Processing Manager initialized successfully');
        } catch (error) {
            console.error('Failed to initialize R2R Processing Manager:', error);
        }
    }
    
    async initializeSignalR() {
        try {
            // Get JWT token from cookie
            const token = this.getJwtToken();

            // Connessione al DocumentUploadHub sul backend
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("http://localhost:5252/hubs/upload", {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Event handlers per aggiornamenti R2R
            this.connection.on("R2RProcessingUpdate", (queueItem) => {
                this.handleR2RUpdate(queueItem);
            });

            this.connection.on("R2RProcessingStatus", (status) => {
                this.handleR2RStatusUpdate(status);
            });

            this.connection.on("InitialUploadSessions", (sessions) => {
                this.handleInitialSessions(sessions);
            });

            // Event handlers per notifiche Admin (Company/User creation)
            this.connection.on("CompanyCreated", (data) => {
                this.handleCompanyCreated(data);
            });

            this.connection.on("CompanyUpdated", (data) => {
                this.handleCompanyUpdated(data);
            });

            this.connection.on("CompanyDeactivated", (data) => {
                this.handleCompanyDeactivated(data);
            });

            this.connection.on("UserCreated", (data) => {
                this.handleUserCreated(data);
            });

            this.connection.on("UserUpdated", (data) => {
                this.handleUserUpdated(data);
            });

            this.connection.on("UserDeactivated", (data) => {
                this.handleUserDeactivated(data);
            });
            
            // Connection state handlers
            this.connection.onreconnecting(() => {
                this.updateConnectionStatus('Reconnecting...', 'warning');
            });

            this.connection.onreconnected(() => {
                this.updateConnectionStatus('Connected', 'success');
                this.requestR2RStatus();
            });

            this.connection.onclose(() => {
                this.isConnected = false;
                this.updateConnectionStatus('Disconnected', 'danger');
            });
            
            // Start connection
            await this.connection.start();
            this.isConnected = true;
            this.updateConnectionStatus('Connected', 'success');

            // Request initial status
            await this.requestR2RStatus();

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.updateConnectionStatus('Connection error', 'danger');
        }
    }
    
    setupUI() {
        // Crea container per status se non esiste
        if (!document.getElementById('r2r-status-container')) {
            this.createStatusContainer();
        }
        
        this.statusContainer = document.getElementById('r2r-status-container');
        this.globalStatusIndicator = document.getElementById('r2r-global-status');
        
        // Aggiungi indicatori di stato alle righe documenti esistenti
        this.enhanceDocumentRows();
    }
    
    createStatusContainer() {
        const container = document.createElement('div');
        container.id = 'r2r-status-container';
        container.className = 'r2r-status-container';
        container.innerHTML = `
            <div class="card mb-4">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">
                        <i class="material-symbols-rounded me-2">cloud_sync</i>
                        R2R Processing Status
                    </h6>
                    <div id="r2r-global-status" class="badge bg-secondary">
                        <i class="material-symbols-rounded me-1">sync</i>
                        Initializing...
                    </div>
                </div>
                <div class="card-body">
                    <div id="r2r-processing-list" class="r2r-processing-list">
                        <div class="text-center text-muted py-3">
                            <i class="material-symbols-rounded" style="font-size: 2rem;">cloud_queue</i>
                            <p class="mb-0 mt-2">No processing in progress</p>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Inserisci prima della tabella documenti
        const documentsTable = document.querySelector('.documents-table, #documents-table');
        if (documentsTable) {
            documentsTable.parentNode.insertBefore(container, documentsTable);
        } else {
            // Fallback: aggiungi all'inizio del contenuto principale
            const mainContent = document.querySelector('.container-fluid, .main-content');
            if (mainContent) {
                mainContent.insertBefore(container, mainContent.firstChild);
            }
        }
    }
    
    enhanceDocumentRows() {
        // Aggiungi indicatori di stato R2R alle righe documenti esistenti
        const documentRows = document.querySelectorAll('[data-document-id]');
        documentRows.forEach(row => {
            const documentId = row.getAttribute('data-document-id');
            if (documentId && !row.querySelector('.r2r-status-indicator')) {
                this.addStatusIndicatorToRow(row, documentId);
            }
        });
    }
    
    addStatusIndicatorToRow(row, documentId) {
        const statusCell = row.querySelector('.document-status, td:last-child');
        if (statusCell) {
            const indicator = document.createElement('div');
            indicator.className = 'r2r-status-indicator ms-2';
            indicator.id = `r2r-status-${documentId}`;
            indicator.innerHTML = `
                <span class="badge bg-light text-dark" title="Stato R2R non disponibile">
                    <i class="material-symbols-rounded me-1" style="font-size: 0.8rem;">help</i>
                    N/A
                </span>
            `;
            statusCell.appendChild(indicator);
        }
    }
    
    handleR2RUpdate(queueItem) {
        console.log('R2R Update received:', queueItem);
        
        // Aggiorna mappa locale
        this.processingItems.set(queueItem.DocumentId, queueItem);
        
        // Aggiorna UI
        this.updateProcessingList();
        this.updateDocumentRowStatus(queueItem);
        this.updateGlobalStatus();
        
        // Mostra notifica per stati importanti
        if (queueItem.Status === 'Completed' || queueItem.Status === 'Failed') {
            this.showStatusNotification(queueItem);
        }
    }
    
    handleR2RStatusUpdate(status) {
        console.log('R2R Status Update received:', status);
        this.updateGlobalStatusFromR2R(status);
    }
    
    handleInitialSessions(sessions) {
        console.log('Initial sessions received:', sessions);
        // Processa sessioni iniziali se necessario
    }

    // Admin notification handlers
    handleCompanyCreated(data) {
        console.log('Company created:', data);
        this.showAdminNotification('success', 'Company Created',
            `Company "${data.Name}" created successfully. R2R tenant sync in progress.`, 'business');

        // Update company pages if we're on them
        if (this.isOnPage(['companies', 'adminusers'])) {
            this.schedulePageRefresh(3000);
        }
    }

    handleCompanyUpdated(data) {
        console.log('Company updated:', data);
        this.showAdminNotification('info', 'Company Updated',
            `Company "${data.Name}" updated successfully. R2R tenant sync in progress.`, 'edit');

        // Update R2R status indicators
        this.updateR2RStatusIndicator('company', data.CompanyId, data.R2RTenantId);
    }

    handleCompanyDeactivated(data) {
        console.log('Company deactivated:', data);
        this.showAdminNotification('warning', 'Company Deactivated',
            `Company "${data.Name}" has been deactivated.`, 'block');

        if (this.isOnPage(['companies'])) {
            this.schedulePageRefresh(2000);
        }
    }

    handleUserCreated(data) {
        console.log('User created:', data);
        this.showAdminNotification('success', 'User Created',
            `User "${data.Email}" created successfully. R2R user sync in progress.`, 'person_add');

        // Update user pages if we're on them
        if (this.isOnPage(['adminusers', 'companyusers'])) {
            this.schedulePageRefresh(3000);
        }
    }

    handleUserUpdated(data) {
        console.log('User updated:', data);
        this.showAdminNotification('info', 'User Updated',
            `User "${data.Email}" updated successfully. R2R user sync in progress.`, 'edit');

        // Update R2R status indicators
        this.updateR2RStatusIndicator('user', data.UserId, data.R2RUserId);
    }

    handleUserDeactivated(data) {
        console.log('User deactivated:', data);
        this.showAdminNotification('warning', 'User Deactivated',
            `User "${data.Email}" has been deactivated.`, 'block');

        if (this.isOnPage(['adminusers', 'companyusers'])) {
            this.schedulePageRefresh(2000);
        }
    }
    
    updateProcessingList() {
        const listContainer = document.getElementById('r2r-processing-list');
        if (!listContainer) return;
        
        const activeItems = Array.from(this.processingItems.values())
            .filter(item => !['Completed', 'Failed'].includes(item.Status))
            .sort((a, b) => new Date(a.CreatedAt) - new Date(b.CreatedAt));
        
        if (activeItems.length === 0) {
            listContainer.innerHTML = `
                <div class="text-center text-muted py-3">
                    <i class="material-symbols-rounded" style="font-size: 2rem;">cloud_done</i>
                    <p class="mb-0 mt-2">No processing in progress</p>
                </div>
            `;
            return;
        }
        
        const itemsHtml = activeItems.map(item => this.createProcessingItemHtml(item)).join('');
        listContainer.innerHTML = itemsHtml;
    }
    
    createProcessingItemHtml(item) {
        const config = this.statusConfig[item.Status] || this.statusConfig['Queued'];
        const progress = this.calculateProgress(item.Status);
        
        return `
            <div class="r2r-processing-item mb-3" data-document-id="${item.DocumentId}">
                <div class="d-flex align-items-center">
                    <div class="r2r-status-icon me-3">
                        <i class="material-symbols-rounded text-${config.color} ${config.animation}">${config.icon}</i>
                    </div>
                    <div class="flex-grow-1">
                        <div class="d-flex justify-content-between align-items-center mb-1">
                            <h6 class="mb-0">${item.FileName}</h6>
                            <span class="badge bg-${config.color}">${item.Status}</span>
                        </div>
                        <p class="text-muted mb-2 small">${config.message}</p>
                        <div class="progress" style="height: 4px;">
                            <div class="progress-bar bg-${config.color}" style="width: ${progress}%"></div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
    
    calculateProgress(status) {
        const progressMap = {
            'Queued': 10,
            'Processing': 25,
            'Chunking': 50,
            'R2RIngestion': 75,
            'Indexing': 90,
            'Completed': 100,
            'Failed': 100,
            'Retrying': 40
        };
        return progressMap[status] || 0;
    }
    
    updateDocumentRowStatus(queueItem) {
        const indicator = document.getElementById(`r2r-status-${queueItem.DocumentId}`);
        if (indicator) {
            const config = this.statusConfig[queueItem.Status] || this.statusConfig['Queued'];
            indicator.innerHTML = `
                <span class="badge bg-${config.color}" title="${config.message}">
                    <i class="material-symbols-rounded me-1 ${config.animation}" style="font-size: 0.8rem;">${config.icon}</i>
                    ${queueItem.Status}
                </span>
            `;
        }
    }
    
    updateGlobalStatus() {
        if (!this.globalStatusIndicator) return;
        
        const activeCount = Array.from(this.processingItems.values())
            .filter(item => !['Completed', 'Failed'].includes(item.Status)).length;
        
        if (activeCount === 0) {
            this.globalStatusIndicator.className = 'badge bg-success';
            this.globalStatusIndicator.innerHTML = `
                <i class="material-symbols-rounded me-1">check_circle</i>
                All completed
            `;
        } else {
            this.globalStatusIndicator.className = 'badge bg-warning';
            this.globalStatusIndicator.innerHTML = `
                <i class="material-symbols-rounded me-1 spin">sync</i>
                ${activeCount} processing
            `;
        }
    }
    
    updateGlobalStatusFromR2R(status) {
        // Aggiorna stato globale basato su informazioni R2R
        if (status && this.globalStatusIndicator) {
            const queueSize = status.QueueSize || 0;
            const rateLimitStatus = status.RateLimitStatus || 'Normal';
            
            if (queueSize > 10) {
                this.globalStatusIndicator.className = 'badge bg-warning';
                this.globalStatusIndicator.innerHTML = `
                    <i class="material-symbols-rounded me-1">queue</i>
                    Queue: ${queueSize}
                `;
            }
        }
    }
    
    showStatusNotification(queueItem) {
        const config = this.statusConfig[queueItem.Status];
        const isSuccess = queueItem.Status === 'Completed';

        // Crea notifica toast
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${config.color} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="material-symbols-rounded me-2">${config.icon}</i>
                    <strong>${queueItem.FileName}</strong> - ${config.message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        this.showToast(toast, 5000);
    }

    showAdminNotification(type, title, message, icon = 'info') {
        const colorMap = {
            'success': 'success',
            'info': 'info',
            'warning': 'warning',
            'error': 'danger'
        };

        const bgColor = colorMap[type] || 'info';

        // Crea notifica toast per admin
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${bgColor} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="material-symbols-rounded me-2">${icon}</i>
                    <strong>${title}</strong><br>
                    <small>${message}</small>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        this.showToast(toast, 7000);
    }

    showToast(toast, delay = 5000) {
        // Aggiungi al container toast
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '1055';
            document.body.appendChild(toastContainer);
        }

        toastContainer.appendChild(toast);

        // Mostra toast
        const bsToast = new bootstrap.Toast(toast, { delay });
        bsToast.show();

        // Rimuovi dopo che si nasconde
        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }
    
    updateConnectionStatus(message, type) {
        console.log(`R2R Connection: ${message} (${type})`);
        
        // Aggiorna indicatore di connessione se esiste
        const connectionIndicator = document.getElementById('r2r-connection-status');
        if (connectionIndicator) {
            connectionIndicator.className = `badge bg-${type}`;
            connectionIndicator.textContent = message;
        }
    }
    
    async requestR2RStatus() {
        if (this.isConnected && this.connection) {
            try {
                await this.connection.invoke("GetR2RProcessingStatus");
            } catch (error) {
                console.error('Failed to request R2R status:', error);
            }
        }
    }
    
    bindEvents() {
        // Refresh button
        document.addEventListener('click', (e) => {
            if (e.target.matches('.r2r-refresh-btn, .r2r-refresh-btn *')) {
                e.preventDefault();
                this.requestR2RStatus();
            }
        });
        
        // Auto-refresh ogni 30 secondi
        setInterval(() => {
            if (this.isConnected) {
                this.requestR2RStatus();
            }
        }, 30000);
    }
    
    // Admin support methods
    isOnPage(pageNames) {
        const currentPath = window.location.pathname.toLowerCase();
        return pageNames.some(page => currentPath.includes(page.toLowerCase()));
    }

    schedulePageRefresh(delay = 3000) {
        // Show refresh notification
        this.showAdminNotification('info', 'Page Refresh',
            `Page will refresh in ${delay/1000} seconds to show updates.`, 'refresh');

        // Schedule refresh
        setTimeout(() => {
            window.location.reload();
        }, delay);
    }

    updateR2RStatusIndicator(type, entityId, r2rId) {
        // Update R2R status indicators on the page
        const indicators = document.querySelectorAll(`[data-${type}-id="${entityId}"] .r2r-status, .r2r-status[data-${type}-id="${entityId}"]`);

        indicators.forEach(indicator => {
            if (r2rId) {
                indicator.innerHTML = `
                    <span class="badge bg-success" title="R2R Synchronized">
                        <i class="material-symbols-rounded me-1" style="font-size: 12px;">check_circle</i>
                        Synced
                    </span>
                    <div class="small text-muted mt-1">ID: ${r2rId.substring(0, 8)}...</div>
                `;
            } else {
                indicator.innerHTML = `
                    <span class="badge bg-warning" title="R2R Sync in Progress">
                        <i class="material-symbols-rounded me-1 spin" style="font-size: 12px;">sync</i>
                        Syncing...
                    </span>
                `;
            }
        });
    }

    // Metodi pubblici per integrazione esterna
    getProcessingStatus(documentId) {
        return this.processingItems.get(documentId);
    }

    isDocumentProcessing(documentId) {
        const item = this.processingItems.get(documentId);
        return item && !['Completed', 'Failed'].includes(item.Status);
    }

    getActiveProcessingCount() {
        return Array.from(this.processingItems.values())
            .filter(item => !['Completed', 'Failed'].includes(item.Status)).length;
    }

    // Get JWT token from authentication cookie
    getJwtToken() {
        // Try to get from meta tag first (set by server)
        const metaToken = document.querySelector('meta[name="jwt-token"]');
        if (metaToken) {
            return metaToken.getAttribute('content');
        }

        // Try to get from localStorage
        const localToken = localStorage.getItem('jwt_token') || localStorage.getItem('access_token');
        if (localToken) {
            return localToken;
        }

        // Try to get from sessionStorage
        const sessionToken = sessionStorage.getItem('jwt_token') || sessionStorage.getItem('access_token');
        if (sessionToken) {
            return sessionToken;
        }

        // Try to parse from cookie (if stored as plain JWT)
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'jwt_token' || name === 'access_token') {
                return decodeURIComponent(value);
            }
        }

        console.warn('JWT token not found for SignalR authentication');
        return '';
    }

}

// Inizializza automaticamente quando il DOM è pronto
document.addEventListener('DOMContentLoaded', () => {
    // Inizializza se siamo in una pagina con documenti o pagine admin
    const isDocumentPage = document.querySelector('.documents-table, #documents-table, [data-page="documents"]');
    const isAdminPage = document.querySelector('[data-page="companies"], [data-page="adminusers"], [data-page="companyusers"]') ||
                       window.location.pathname.toLowerCase().includes('/companies') ||
                       window.location.pathname.toLowerCase().includes('/adminusers') ||
                       window.location.pathname.toLowerCase().includes('/companyusers');

    if (isDocumentPage || isAdminPage) {
        window.r2rManager = new R2RProcessingManager();
    }
});

// Export per uso esterno
window.R2RProcessingManager = R2RProcessingManager;
