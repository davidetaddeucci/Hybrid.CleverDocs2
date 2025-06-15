// Document Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeDocumentManagement();
});

function initializeDocumentManagement() {
    initializeSearchSuggestions();
    initializeViewToggle();
    initializeInfiniteScroll();
    initializeKeyboardShortcuts();
    initializeBulkActions();
}

// Search Suggestions
function initializeSearchSuggestions() {
    const searchInput = document.querySelector('.search-input');
    const suggestionsContainer = document.getElementById('searchSuggestions');
    
    if (!searchInput || !suggestionsContainer) return;

    let searchTimeout;
    let currentSuggestions = [];

    searchInput.addEventListener('input', function() {
        const query = this.value.trim();
        
        clearTimeout(searchTimeout);
        
        if (query.length < 2) {
            hideSuggestions();
            return;
        }

        searchTimeout = setTimeout(() => {
            fetchSearchSuggestions(query);
        }, 300);
    });

    searchInput.addEventListener('keydown', function(e) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            navigateSuggestions(e.key === 'ArrowDown' ? 1 : -1);
        } else if (e.key === 'Enter') {
            const activeItem = suggestionsContainer.querySelector('.suggestion-item.active');
            if (activeItem) {
                e.preventDefault();
                selectSuggestion(activeItem.textContent);
            }
        } else if (e.key === 'Escape') {
            hideSuggestions();
        }
    });

    // Hide suggestions when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !suggestionsContainer.contains(e.target)) {
            hideSuggestions();
        }
    });

    async function fetchSearchSuggestions(query) {
        try {
            const url = searchInput.dataset.suggestionsUrl;
            if (!url) return;

            const response = await fetch(`${url}?term=${encodeURIComponent(query)}`);
            const suggestions = await response.json();
            
            displaySuggestions(suggestions);
        } catch (error) {
            console.error('Error fetching search suggestions:', error);
        }
    }

    function displaySuggestions(suggestions) {
        currentSuggestions = suggestions;
        
        if (suggestions.length === 0) {
            hideSuggestions();
            return;
        }

        suggestionsContainer.innerHTML = suggestions.map(suggestion => 
            `<button type="button" class="suggestion-item" onclick="selectSuggestion('${suggestion}')">
                <i class="icon-search"></i>
                ${suggestion}
            </button>`
        ).join('');

        suggestionsContainer.style.display = 'block';
    }

    function hideSuggestions() {
        suggestionsContainer.style.display = 'none';
        suggestionsContainer.innerHTML = '';
        currentSuggestions = [];
    }

    function navigateSuggestions(direction) {
        const items = suggestionsContainer.querySelectorAll('.suggestion-item');
        if (items.length === 0) return;

        const activeItem = suggestionsContainer.querySelector('.suggestion-item.active');
        let newIndex = 0;

        if (activeItem) {
            const currentIndex = Array.from(items).indexOf(activeItem);
            newIndex = currentIndex + direction;
            activeItem.classList.remove('active');
        }

        // Wrap around
        if (newIndex < 0) newIndex = items.length - 1;
        if (newIndex >= items.length) newIndex = 0;

        items[newIndex].classList.add('active');
    }

    window.selectSuggestion = function(suggestion) {
        searchInput.value = suggestion;
        hideSuggestions();
        document.getElementById('searchForm').submit();
    };
}

// View Toggle
function initializeViewToggle() {
    window.toggleViewMode = function(mode) {
        const viewModeInput = document.getElementById('viewMode');
        const documentsContainer = document.getElementById('documentsContainer');
        
        if (viewModeInput) {
            viewModeInput.value = mode;
        }
        
        if (documentsContainer) {
            documentsContainer.className = `documents-container ${mode}-view`;
        }
        
        // Update toggle buttons
        document.querySelectorAll('.view-toggle .btn').forEach(btn => {
            btn.classList.remove('active');
        });
        
        event.target.classList.add('active');
        
        // Save preference
        localStorage.setItem('documentViewMode', mode);
        
        // Optionally submit form to persist server-side
        // document.getElementById('searchForm').submit();
    };

    // Load saved view mode
    const savedViewMode = localStorage.getItem('documentViewMode');
    if (savedViewMode) {
        const viewModeInput = document.getElementById('viewMode');
        if (viewModeInput && !viewModeInput.value) {
            viewModeInput.value = savedViewMode;
        }
    }
}

// Sort Direction Toggle
window.toggleSortDirection = function() {
    const sortDirectionInput = document.getElementById('sortDirection');
    const currentDirection = sortDirectionInput.value;
    const newDirection = currentDirection === 'Asc' ? 'Desc' : 'Asc';
    
    sortDirectionInput.value = newDirection;
    
    // Update button icon
    const button = event.target.closest('button');
    const icon = button.querySelector('i');
    icon.className = `icon-arrow-${newDirection === 'Desc' ? 'down' : 'up'}`;
    
    // Submit form
    document.getElementById('searchForm').submit();
};

// Clear Search
window.clearSearch = function() {
    const searchInput = document.querySelector('input[name="SearchTerm"]');
    if (searchInput) {
        searchInput.value = '';
        document.getElementById('searchForm').submit();
    }
};

// Infinite Scroll (for large document lists)
function initializeInfiniteScroll() {
    let isLoading = false;
    let hasMorePages = true;
    
    const pagination = document.querySelector('.pagination-container');
    if (!pagination) return; // No pagination means no infinite scroll needed

    // Check if there are more pages
    const currentPageElement = pagination.querySelector('.page-item.active .page-link');
    const lastPageElement = pagination.querySelector('.page-link[data-page]:last-of-type');
    
    if (currentPageElement && lastPageElement) {
        const currentPage = parseInt(currentPageElement.textContent);
        const lastPage = parseInt(lastPageElement.dataset.page);
        hasMorePages = currentPage < lastPage;
    }

    if (!hasMorePages) return;

    window.addEventListener('scroll', function() {
        if (isLoading || !hasMorePages) return;

        const scrollPosition = window.innerHeight + window.scrollY;
        const documentHeight = document.documentElement.offsetHeight;
        
        // Load more when 200px from bottom
        if (scrollPosition >= documentHeight - 200) {
            loadMoreDocuments();
        }
    });

    async function loadMoreDocuments() {
        isLoading = true;
        
        try {
            // Show loading indicator
            showLoadingIndicator();
            
            // Get next page URL from pagination
            const nextPageLink = pagination.querySelector('.page-item:not(.disabled) .page-link[aria-label="Next"]');
            if (!nextPageLink) {
                hasMorePages = false;
                return;
            }
            
            const nextPageUrl = nextPageLink.href;
            const response = await fetch(nextPageUrl, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
            
            if (!response.ok) throw new Error('Failed to load more documents');
            
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            // Extract new documents
            const newDocuments = doc.querySelectorAll('.document-card, .list-item');
            const documentsContainer = document.querySelector('.documents-grid, .documents-list');
            
            // Append new documents with animation
            newDocuments.forEach((doc, index) => {
                setTimeout(() => {
                    documentsContainer.appendChild(doc);
                }, index * 50); // Stagger animation
            });
            
            // Update pagination
            const newPagination = doc.querySelector('.pagination-container');
            if (newPagination) {
                pagination.innerHTML = newPagination.innerHTML;
            } else {
                hasMorePages = false;
            }
            
        } catch (error) {
            console.error('Error loading more documents:', error);
            showNotification('Failed to load more documents', 'error');
        } finally {
            isLoading = false;
            hideLoadingIndicator();
        }
    }

    function showLoadingIndicator() {
        const indicator = document.createElement('div');
        indicator.id = 'loadingIndicator';
        indicator.className = 'loading-indicator';
        indicator.innerHTML = `
            <div class="spinner"></div>
            <span>Loading more documents...</span>
        `;
        document.querySelector('.documents-content').appendChild(indicator);
    }

    function hideLoadingIndicator() {
        const indicator = document.getElementById('loadingIndicator');
        if (indicator) {
            indicator.remove();
        }
    }
}

// Keyboard Shortcuts
function initializeKeyboardShortcuts() {
    document.addEventListener('keydown', function(e) {
        // Ctrl/Cmd + F: Focus search
        if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
            e.preventDefault();
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.focus();
                searchInput.select();
            }
        }
        
        // Ctrl/Cmd + U: Go to upload
        if ((e.ctrlKey || e.metaKey) && e.key === 'u') {
            e.preventDefault();
            const uploadLink = document.querySelector('a[href*="Upload"]');
            if (uploadLink) {
                window.location.href = uploadLink.href;
            }
        }
        
        // G then D: Go to documents
        if (e.key === 'g' && !e.ctrlKey && !e.metaKey) {
            document.addEventListener('keydown', function gThenD(e2) {
                if (e2.key === 'd') {
                    window.location.href = '/Documents';
                }
                document.removeEventListener('keydown', gThenD);
            }, { once: true });
        }
        
        // Escape: Clear search or close modals
        if (e.key === 'Escape') {
            const searchInput = document.querySelector('.search-input');
            if (searchInput && searchInput === document.activeElement) {
                searchInput.blur();
            }
            
            // Close any open dropdowns
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
        }
    });
}

// Bulk Actions (for future implementation)
function initializeBulkActions() {
    let selectedDocuments = new Set();
    
    // Document selection
    document.addEventListener('click', function(e) {
        if (e.target.matches('.document-checkbox')) {
            const documentId = e.target.dataset.documentId;
            
            if (e.target.checked) {
                selectedDocuments.add(documentId);
            } else {
                selectedDocuments.delete(documentId);
            }
            
            updateBulkActionsToolbar();
        }
    });
    
    // Select all
    window.selectAllDocuments = function() {
        const checkboxes = document.querySelectorAll('.document-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = true;
            selectedDocuments.add(checkbox.dataset.documentId);
        });
        updateBulkActionsToolbar();
    };
    
    // Clear selection
    window.clearSelection = function() {
        const checkboxes = document.querySelectorAll('.document-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = false;
        });
        selectedDocuments.clear();
        updateBulkActionsToolbar();
    };
    
    function updateBulkActionsToolbar() {
        const toolbar = document.getElementById('bulkActionsToolbar');
        const count = selectedDocuments.size;
        
        if (count > 0) {
            if (!toolbar) {
                createBulkActionsToolbar();
            }
            document.getElementById('selectedCount').textContent = count;
            toolbar.style.display = 'flex';
        } else if (toolbar) {
            toolbar.style.display = 'none';
        }
    }
    
    function createBulkActionsToolbar() {
        const toolbar = document.createElement('div');
        toolbar.id = 'bulkActionsToolbar';
        toolbar.className = 'bulk-actions-toolbar';
        toolbar.innerHTML = `
            <div class="selection-info">
                <span id="selectedCount">0</span> documents selected
                <button type="button" onclick="clearSelection()" class="btn-link">Clear</button>
            </div>
            <div class="bulk-actions">
                <button type="button" class="btn btn-outline btn-sm" onclick="bulkDownload()">
                    <i class="icon-download"></i> Download
                </button>
                <button type="button" class="btn btn-outline btn-sm" onclick="bulkMove()">
                    <i class="icon-folder-move"></i> Move
                </button>
                <button type="button" class="btn btn-outline btn-sm btn-danger" onclick="bulkDelete()">
                    <i class="icon-trash"></i> Delete
                </button>
            </div>
        `;
        
        document.querySelector('.documents-content').insertBefore(toolbar, document.querySelector('.documents-container'));
    }
}

// Utility Functions
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="icon-${type === 'error' ? 'alert-circle' : type === 'success' ? 'check-circle' : 'info'}"></i>
            <span>${message}</span>
            <button type="button" class="notification-close" onclick="this.parentElement.parentElement.remove()">
                <i class="icon-x"></i>
            </button>
        </div>
    `;
    
    document.body.appendChild(notification);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 5000);
}

// Document Actions
window.previewDocument = function(documentId) {
    // Open preview in modal or new tab
    const previewUrl = `/Documents/${documentId}/preview`;
    window.open(previewUrl, '_blank');
};

window.downloadDocument = function(documentId) {
    // Trigger download
    const downloadUrl = `/Documents/${documentId}/download`;
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = '';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Performance Optimizations
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Lazy Loading for Images
function initializeLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');
    
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
                observer.unobserve(img);
            }
        });
    });
    
    images.forEach(img => imageObserver.observe(img));
}

// Initialize lazy loading when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeLazyLoading);
} else {
    initializeLazyLoading();
}
