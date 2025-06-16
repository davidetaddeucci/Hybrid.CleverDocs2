/**
 * Advanced Search Manager for Documents
 * Provides autocomplete, search history, saved searches, and advanced filtering
 */
class AdvancedSearchManager {
    constructor() {
        this.searchInput = null;
        this.searchForm = null;
        this.searchHistory = [];
        this.savedSearches = [];
        this.currentFilters = {};
        this.debounceTimer = null;
        this.debounceDelay = 300;
        
        this.init();
    }

    init() {
        this.searchInput = document.getElementById('searchTerm');
        this.searchForm = document.getElementById('documentsSearchForm');
        
        if (this.searchInput) {
            this.setupAutoComplete();
            this.setupSearchHistory();
            this.setupKeyboardShortcuts();
        }
        
        this.setupAdvancedFilters();
        this.setupSavedSearches();
        this.loadSearchHistory();
        this.loadSavedSearches();
    }

    /**
     * Setup autocomplete functionality
     */
    setupAutoComplete() {
        // Create autocomplete dropdown
        const autocompleteContainer = document.createElement('div');
        autocompleteContainer.className = 'autocomplete-container position-relative';
        autocompleteContainer.innerHTML = `
            <div class="autocomplete-dropdown d-none" id="autocompleteDropdown">
                <div class="autocomplete-section">
                    <div class="autocomplete-header">Suggestions</div>
                    <div class="autocomplete-items" id="autocompleteSuggestions"></div>
                </div>
                <div class="autocomplete-section">
                    <div class="autocomplete-header">Recent Searches</div>
                    <div class="autocomplete-items" id="autocompleteHistory"></div>
                </div>
                <div class="autocomplete-section">
                    <div class="autocomplete-header">Saved Searches</div>
                    <div class="autocomplete-items" id="autocompleteSaved"></div>
                </div>
            </div>
        `;
        
        this.searchInput.parentNode.appendChild(autocompleteContainer);
        this.autocompleteDropdown = document.getElementById('autocompleteDropdown');

        // Setup input events
        this.searchInput.addEventListener('input', (e) => {
            this.handleSearchInput(e.target.value);
        });

        this.searchInput.addEventListener('focus', () => {
            this.showAutocomplete();
        });

        this.searchInput.addEventListener('blur', (e) => {
            // Delay hiding to allow clicking on dropdown items
            setTimeout(() => this.hideAutocomplete(), 150);
        });

        // Setup keyboard navigation
        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeyNavigation(e);
        });
    }

    /**
     * Handle search input with debouncing
     */
    handleSearchInput(value) {
        clearTimeout(this.debounceTimer);
        
        this.debounceTimer = setTimeout(async () => {
            if (value.length >= 2) {
                await this.fetchSuggestions(value);
            } else {
                this.updateAutocompleteContent();
            }
        }, this.debounceDelay);
    }

    /**
     * Fetch search suggestions from API
     */
    async fetchSuggestions(term) {
        try {
            const response = await fetch(`/api/documents/search/suggestions?term=${encodeURIComponent(term)}&limit=5`);
            if (response.ok) {
                const suggestions = await response.json();
                this.updateAutocompleteSuggestions(suggestions);
            } else {
                // Fallback to mock suggestions if API not available
                const mockSuggestions = this.generateMockSuggestions(term);
                this.updateAutocompleteSuggestions(mockSuggestions);
            }
        } catch (error) {
            console.error('Error fetching suggestions:', error);
            // Fallback to mock suggestions
            const mockSuggestions = this.generateMockSuggestions(term);
            this.updateAutocompleteSuggestions(mockSuggestions);
        }
    }

    /**
     * Generate mock suggestions for testing
     */
    generateMockSuggestions(term) {
        const mockData = [
            'Document Analysis', 'Data Processing', 'Report Generation',
            'User Manual', 'Technical Specification', 'Project Plan',
            'Meeting Notes', 'Financial Report', 'Marketing Strategy'
        ];

        return mockData
            .filter(item => item.toLowerCase().includes(term.toLowerCase()))
            .slice(0, 5);
    }

    /**
     * Update autocomplete suggestions
     */
    updateAutocompleteSuggestions(suggestions) {
        const suggestionsContainer = document.getElementById('autocompleteSuggestions');
        
        suggestionsContainer.innerHTML = suggestions.map(suggestion => `
            <div class="autocomplete-item" data-value="${suggestion}">
                <i class="material-symbols-rounded me-2">search</i>
                ${this.highlightMatch(suggestion, this.searchInput.value)}
            </div>
        `).join('');

        // Add click handlers
        suggestionsContainer.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', () => {
                this.selectSuggestion(item.dataset.value);
            });
        });

        this.updateAutocompleteContent();
    }

    /**
     * Highlight matching text in suggestions
     */
    highlightMatch(text, query) {
        const regex = new RegExp(`(${query})`, 'gi');
        return text.replace(regex, '<strong>$1</strong>');
    }

    /**
     * Select a suggestion
     */
    selectSuggestion(value) {
        this.searchInput.value = value;
        this.hideAutocomplete();
        this.performSearch();
    }

    /**
     * Setup search history functionality
     */
    setupSearchHistory() {
        // Load from localStorage as fallback
        const stored = localStorage.getItem('documentSearchHistory');
        if (stored) {
            this.searchHistory = JSON.parse(stored);
        }
    }

    /**
     * Load search history from API
     */
    async loadSearchHistory() {
        try {
            const response = await fetch('/api/documents/search/history?limit=10');
            this.searchHistory = await response.json();
            this.updateAutocompleteContent();
        } catch (error) {
            console.error('Error loading search history:', error);
        }
    }

    /**
     * Record a search in history
     */
    async recordSearch(searchTerm, filters, resultCount) {
        try {
            await fetch('/api/documents/search/history', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    searchTerm,
                    filters,
                    resultCount
                })
            });
            
            // Refresh history
            await this.loadSearchHistory();
        } catch (error) {
            console.error('Error recording search:', error);
        }
    }

    /**
     * Setup saved searches functionality
     */
    setupSavedSearches() {
        // Add save search button
        const saveButton = document.createElement('button');
        saveButton.type = 'button';
        saveButton.className = 'btn btn-outline-secondary btn-sm ms-2';
        saveButton.innerHTML = '<i class="material-symbols-rounded">bookmark_add</i>';
        saveButton.title = 'Save Search';
        saveButton.addEventListener('click', () => this.showSaveSearchModal());
        
        const searchButton = this.searchForm.querySelector('button[type="submit"]');
        searchButton.parentNode.appendChild(saveButton);
    }

    /**
     * Load saved searches from API
     */
    async loadSavedSearches() {
        try {
            const response = await fetch('/api/documents/search/saved');
            this.savedSearches = await response.json();
            this.updateAutocompleteContent();
        } catch (error) {
            console.error('Error loading saved searches:', error);
        }
    }

    /**
     * Show save search modal
     */
    showSaveSearchModal() {
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Save Search</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <form id="saveSearchForm">
                            <div class="mb-3">
                                <label for="searchName" class="form-label">Search Name</label>
                                <input type="text" class="form-control" id="searchName" required>
                            </div>
                            <div class="mb-3">
                                <label for="searchDescription" class="form-label">Description (optional)</label>
                                <textarea class="form-control" id="searchDescription" rows="2"></textarea>
                            </div>
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="isPublic">
                                <label class="form-check-label" for="isPublic">
                                    Make this search public
                                </label>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="advancedSearchManager.saveCurrentSearch()">Save Search</button>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
        
        // Clean up modal after hiding
        modal.addEventListener('hidden.bs.modal', () => {
            document.body.removeChild(modal);
        });
    }

    /**
     * Save current search
     */
    async saveCurrentSearch() {
        const name = document.getElementById('searchName').value;
        const description = document.getElementById('searchDescription').value;
        const isPublic = document.getElementById('isPublic').checked;
        
        if (!name.trim()) {
            alert('Please enter a name for the search');
            return;
        }

        try {
            const response = await fetch('/api/documents/search/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    name: name.trim(),
                    description: description.trim() || null,
                    searchTerm: this.searchInput.value,
                    filters: this.getCurrentFilters(),
                    isPublic: isPublic,
                    isFavorite: false
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.querySelector('.modal'));
                modal.hide();
                
                // Refresh saved searches
                await this.loadSavedSearches();
                
                // Show success message
                this.showToast('Search saved successfully', 'success');
            } else {
                alert(result.message || 'Failed to save search');
            }
        } catch (error) {
            console.error('Error saving search:', error);
            alert('An error occurred while saving the search');
        }
    }

    /**
     * Get current filter values
     */
    getCurrentFilters() {
        const formData = new FormData(this.searchForm);
        const filters = {};
        
        for (const [key, value] of formData.entries()) {
            if (value && key !== 'SearchTerm') {
                filters[key] = value;
            }
        }
        
        return filters;
    }

    /**
     * Setup keyboard shortcuts
     */
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ctrl/Cmd + K to focus search
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                this.searchInput.focus();
            }
            
            // Escape to clear search
            if (e.key === 'Escape' && document.activeElement === this.searchInput) {
                this.searchInput.value = '';
                this.hideAutocomplete();
            }
        });
    }

    /**
     * Handle keyboard navigation in autocomplete
     */
    handleKeyNavigation(e) {
        const items = this.autocompleteDropdown.querySelectorAll('.autocomplete-item');
        const activeItem = this.autocompleteDropdown.querySelector('.autocomplete-item.active');
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.navigateAutocomplete(items, activeItem, 1);
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.navigateAutocomplete(items, activeItem, -1);
                break;
            case 'Enter':
                if (activeItem) {
                    e.preventDefault();
                    this.selectSuggestion(activeItem.dataset.value);
                }
                break;
            case 'Escape':
                this.hideAutocomplete();
                break;
        }
    }

    /**
     * Navigate autocomplete items with keyboard
     */
    navigateAutocomplete(items, activeItem, direction) {
        if (items.length === 0) return;
        
        let newIndex = 0;
        
        if (activeItem) {
            activeItem.classList.remove('active');
            const currentIndex = Array.from(items).indexOf(activeItem);
            newIndex = (currentIndex + direction + items.length) % items.length;
        }
        
        items[newIndex].classList.add('active');
    }

    /**
     * Show autocomplete dropdown
     */
    showAutocomplete() {
        this.updateAutocompleteContent();
        this.autocompleteDropdown.classList.remove('d-none');
    }

    /**
     * Hide autocomplete dropdown
     */
    hideAutocomplete() {
        this.autocompleteDropdown.classList.add('d-none');
    }

    /**
     * Update autocomplete content
     */
    updateAutocompleteContent() {
        this.updateAutocompleteHistory();
        this.updateAutocompleteSaved();
    }

    /**
     * Update autocomplete history section
     */
    updateAutocompleteHistory() {
        const historyContainer = document.getElementById('autocompleteHistory');
        
        historyContainer.innerHTML = this.searchHistory.slice(0, 5).map(item => `
            <div class="autocomplete-item" data-value="${item.searchTerm}">
                <i class="material-symbols-rounded me-2">history</i>
                ${item.searchTerm}
                <small class="text-muted ms-auto">${item.resultCount} results</small>
            </div>
        `).join('');

        // Add click handlers
        historyContainer.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', () => {
                this.selectSuggestion(item.dataset.value);
            });
        });
    }

    /**
     * Update autocomplete saved searches section
     */
    updateAutocompleteSaved() {
        const savedContainer = document.getElementById('autocompleteSaved');
        
        savedContainer.innerHTML = this.savedSearches.slice(0, 5).map(item => `
            <div class="autocomplete-item" data-search-id="${item.id}">
                <i class="material-symbols-rounded me-2">bookmark</i>
                ${item.name}
                <small class="text-muted ms-auto">${item.useCount} uses</small>
            </div>
        `).join('');

        // Add click handlers
        savedContainer.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', () => {
                this.loadSavedSearch(item.dataset.searchId);
            });
        });
    }

    /**
     * Load a saved search
     */
    async loadSavedSearch(searchId) {
        try {
            const response = await fetch(`/api/documents/search/saved/${searchId}`);
            const savedSearch = await response.json();
            
            if (savedSearch) {
                // Load search term
                this.searchInput.value = savedSearch.searchTerm;
                
                // Load filters
                const filters = JSON.parse(savedSearch.filters || '{}');
                this.applyFilters(filters);
                
                // Perform search
                this.performSearch();
                
                this.hideAutocomplete();
            }
        } catch (error) {
            console.error('Error loading saved search:', error);
        }
    }

    /**
     * Apply filters to form
     */
    applyFilters(filters) {
        Object.entries(filters).forEach(([key, value]) => {
            const input = this.searchForm.querySelector(`[name="${key}"]`);
            if (input) {
                input.value = value;
            }
        });
    }

    /**
     * Perform search
     */
    performSearch() {
        this.searchForm.submit();
    }

    /**
     * Setup advanced filters
     */
    setupAdvancedFilters() {
        // Add advanced filters toggle
        const filtersToggle = document.createElement('button');
        filtersToggle.type = 'button';
        filtersToggle.className = 'btn btn-outline-secondary btn-sm';
        filtersToggle.innerHTML = '<i class="material-symbols-rounded">tune</i> Advanced';
        filtersToggle.addEventListener('click', () => this.toggleAdvancedFilters());
        
        const searchButton = this.searchForm.querySelector('button[type="submit"]');
        searchButton.parentNode.insertBefore(filtersToggle, searchButton);
    }

    /**
     * Toggle advanced filters panel
     */
    toggleAdvancedFilters() {
        // Implementation for advanced filters panel
        console.log('Toggle advanced filters');
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'info') {
        // Simple toast implementation
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.textContent = message;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            document.body.removeChild(toast);
        }, 3000);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.advancedSearchManager = new AdvancedSearchManager();
});
