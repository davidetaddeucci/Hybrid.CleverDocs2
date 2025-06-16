/**
 * Collections Management JavaScript
 * Enterprise-grade functionality with Redis caching and real-time updates
 */

class CollectionsManager {
    constructor() {
        this.selectedCollections = new Set();
        this.searchTimeout = null;
        this.currentViewMode = this.getViewMode();
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupViewToggle();
        this.setupSearch();
        this.setupBulkActions();
        this.loadViewMode();
    }

    setupEventListeners() {
        // Filter toggle
        const filterToggle = document.getElementById('filterToggle');
        if (filterToggle) {
            filterToggle.addEventListener('click', () => this.toggleFilters());
        }

        // Collection checkboxes
        document.addEventListener('change', (e) => {
            if (e.target.classList.contains('collection-checkbox')) {
                this.handleCollectionSelect(e.target);
            }
        });

        // Select all checkbox
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.addEventListener('change', (e) => this.selectAll(e.target.checked));
        }

        // Search input
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => this.handleSearch(e.target.value));
            searchInput.addEventListener('focus', () => this.showSearchSuggestions());
            searchInput.addEventListener('blur', () => {
                // Delay hiding to allow clicking on suggestions
                setTimeout(() => this.hideSearchSuggestions(), 200);
            });
        }

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => this.handleKeyboardShortcuts(e));
    }

    setupViewToggle() {
        const gridBtn = document.getElementById('gridViewBtn');
        const listBtn = document.getElementById('listViewBtn');

        if (gridBtn) {
            gridBtn.addEventListener('click', () => this.setViewMode('grid'));
        }
        if (listBtn) {
            listBtn.addEventListener('click', () => this.setViewMode('list'));
        }
    }

    setupSearch() {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            // Debounced search suggestions
            searchInput.addEventListener('input', (e) => {
                clearTimeout(this.searchTimeout);
                this.searchTimeout = setTimeout(() => {
                    this.fetchSearchSuggestions(e.target.value);
                }, 300);
            });
        }
    }

    setupBulkActions() {
        // Initialize bulk actions state
        this.updateBulkActionsVisibility();
    }

    // View Mode Management
    setViewMode(mode) {
        this.currentViewMode = mode;
        localStorage.setItem('collectionViewMode', mode);
        
        const container = document.getElementById('collectionsContainer');
        if (container) {
            container.className = `collections-container ${mode}-view`;
        }

        // Update button states
        document.querySelectorAll('.view-toggle .btn').forEach(btn => {
            btn.classList.remove('active');
        });
        
        const activeBtn = document.querySelector(`[data-view="${mode}"]`);
        if (activeBtn) {
            activeBtn.classList.add('active');
        }
    }

    getViewMode() {
        return localStorage.getItem('collectionViewMode') || 'grid';
    }

    loadViewMode() {
        this.setViewMode(this.currentViewMode);
    }

    // Search Functionality
    async fetchSearchSuggestions(term) {
        if (!term || term.length < 2) {
            this.hideSearchSuggestions();
            return;
        }

        try {
            const response = await fetch(`/Collections/search-suggestions?term=${encodeURIComponent(term)}`);
            if (response.ok) {
                const suggestions = await response.json();
                this.displaySearchSuggestions(suggestions);
            }
        } catch (error) {
            console.error('Error fetching search suggestions:', error);
        }
    }

    displaySearchSuggestions(suggestions) {
        const suggestionsContainer = document.getElementById('searchSuggestions');
        if (!suggestionsContainer) return;

        if (suggestions.length === 0) {
            this.hideSearchSuggestions();
            return;
        }

        suggestionsContainer.innerHTML = suggestions
            .map(suggestion => `
                <div class="suggestion-item" onclick="collectionsManager.selectSuggestion('${suggestion}')">
                    <i class="fas fa-search"></i>
                    <span>${suggestion}</span>
                </div>
            `).join('');

        suggestionsContainer.style.display = 'block';
    }

    selectSuggestion(suggestion) {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.value = suggestion;
            this.hideSearchSuggestions();
            // Trigger search
            searchInput.closest('form').submit();
        }
    }

    showSearchSuggestions() {
        const suggestionsContainer = document.getElementById('searchSuggestions');
        if (suggestionsContainer && suggestionsContainer.children.length > 0) {
            suggestionsContainer.style.display = 'block';
        }
    }

    hideSearchSuggestions() {
        const suggestionsContainer = document.getElementById('searchSuggestions');
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
        }
    }

    handleSearch(term) {
        // Real-time search feedback
        if (term.length >= 2) {
            this.fetchSearchSuggestions(term);
        } else {
            this.hideSearchSuggestions();
        }
    }

    // Filter Management
    toggleFilters() {
        const filtersPanel = document.getElementById('filtersPanel');
        if (filtersPanel) {
            filtersPanel.classList.toggle('show');
        }
    }

    // Collection Selection
    handleCollectionSelect(checkbox) {
        const collectionId = checkbox.value;
        
        if (checkbox.checked) {
            this.selectedCollections.add(collectionId);
        } else {
            this.selectedCollections.delete(collectionId);
        }

        this.updateBulkActionsVisibility();
        this.updateSelectedCount();
    }

    selectAll(checked) {
        const checkboxes = document.querySelectorAll('.collection-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = checked;
            this.handleCollectionSelect(checkbox);
        });
    }

    updateBulkActionsVisibility() {
        const bulkActions = document.getElementById('bulkActions');
        if (bulkActions) {
            bulkActions.style.display = this.selectedCollections.size > 0 ? 'flex' : 'none';
        }
    }

    updateSelectedCount() {
        const selectedCount = document.getElementById('selectedCount');
        if (selectedCount) {
            const count = this.selectedCollections.size;
            selectedCount.textContent = `${count} selected`;
        }
    }

    // Sorting
    updateSort() {
        const sortBy = document.getElementById('sortBy').value;
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('SortBy', sortBy);
        currentUrl.searchParams.set('Page', '1'); // Reset to first page
        window.location.href = currentUrl.toString();
    }

    toggleSortDirection() {
        const sortBtn = document.querySelector('.sort-direction');
        const currentDirection = sortBtn.dataset.direction;
        const newDirection = currentDirection === 'ASC' ? 'DESC' : 'ASC';
        
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('SortDirection', newDirection);
        currentUrl.searchParams.set('Page', '1'); // Reset to first page
        window.location.href = currentUrl.toString();
    }

    // Keyboard Shortcuts
    handleKeyboardShortcuts(e) {
        // Ctrl+F: Focus search
        if (e.ctrlKey && e.key === 'f') {
            e.preventDefault();
            const searchInput = document.getElementById('searchInput');
            if (searchInput) {
                searchInput.focus();
            }
        }

        // Ctrl+N: New collection
        if (e.ctrlKey && e.key === 'n') {
            e.preventDefault();
            window.location.href = '/Collections/Create';
        }

        // Escape: Clear selection
        if (e.key === 'Escape') {
            this.clearSelection();
            this.hideSearchSuggestions();
        }
    }

    clearSelection() {
        this.selectedCollections.clear();
        document.querySelectorAll('.collection-checkbox').forEach(checkbox => {
            checkbox.checked = false;
        });
        this.updateBulkActionsVisibility();
        this.updateSelectedCount();
    }
}

// Collection Actions
async function toggleFavorite(collectionId) {
    try {
        const response = await fetch(`/Collections/${collectionId}/toggle-favorite`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                // Update UI
                const favoriteBtn = document.querySelector(`[onclick="toggleFavorite('${collectionId}')"]`);
                if (favoriteBtn) {
                    favoriteBtn.classList.toggle('active');
                    const icon = favoriteBtn.querySelector('i');
                    if (favoriteBtn.classList.contains('active')) {
                        favoriteBtn.title = 'Remove from favorites';
                        icon.style.color = '#ffc107';
                    } else {
                        favoriteBtn.title = 'Add to favorites';
                        icon.style.color = '';
                    }
                }
                
                // Show success message
                showNotification('Favorite status updated successfully', 'success');
            } else {
                showNotification('Failed to update favorite status', 'error');
            }
        }
    } catch (error) {
        console.error('Error toggling favorite:', error);
        showNotification('An error occurred while updating favorite status', 'error');
    }
}

function deleteCollection(collectionId, collectionName) {
    // Set up delete modal
    document.getElementById('deleteCollectionName').textContent = collectionName;
    document.getElementById('deleteForm').action = `/Collections/${collectionId}/delete`;
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
    modal.show();
}

async function bulkDelete() {
    if (collectionsManager.selectedCollections.size === 0) return;

    const confirmed = confirm(`Are you sure you want to delete ${collectionsManager.selectedCollections.size} collection(s)?`);
    if (!confirmed) return;

    try {
        const response = await fetch('/Collections/bulk-operation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                collectionIds: Array.from(collectionsManager.selectedCollections),
                operation: 'delete',
                parameters: {}
            })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                showNotification('Collections deleted successfully', 'success');
                window.location.reload();
            } else {
                showNotification('Failed to delete collections', 'error');
            }
        }
    } catch (error) {
        console.error('Error deleting collections:', error);
        showNotification('An error occurred while deleting collections', 'error');
    }
}

async function bulkFavorite() {
    if (collectionsManager.selectedCollections.size === 0) return;

    try {
        const response = await fetch('/Collections/bulk-operation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                collectionIds: Array.from(collectionsManager.selectedCollections),
                operation: 'favorite',
                parameters: {}
            })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                showNotification('Favorite status updated successfully', 'success');
                window.location.reload();
            } else {
                showNotification('Failed to update favorite status', 'error');
            }
        }
    } catch (error) {
        console.error('Error updating favorites:', error);
        showNotification('An error occurred while updating favorites', 'error');
    }
}

function shareCollection(collectionId) {
    // TODO: Implement sharing functionality
    showNotification('Sharing functionality coming soon', 'info');
}

// Utility Functions
function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show notification`;
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    // Add to page
    const container = document.querySelector('.collections-page') || document.body;
    container.insertBefore(notification, container.firstChild);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

// Global functions for sorting (called from HTML)
function updateSort() {
    collectionsManager.updateSort();
}

function toggleSortDirection() {
    collectionsManager.toggleSortDirection();
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.collectionsManager = new CollectionsManager();
});
