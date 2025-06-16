/**
 * Multi-Column Sorting Manager for DataGrid
 * Provides advanced sorting capabilities with multiple columns, priorities, and visual indicators
 */
class MultiColumnSortManager {
    constructor(tableSelector = '#documentsTable') {
        this.table = document.querySelector(tableSelector);
        this.sortColumns = [];
        this.maxSortColumns = 3;
        this.sortIndicators = new Map();
        
        this.init();
    }

    init() {
        if (!this.table) {
            console.warn('Table not found for multi-column sorting');
            return;
        }

        this.setupColumnHeaders();
        this.createSortPanel();
        this.loadSortState();
        this.updateSortIndicators();
    }

    /**
     * Setup clickable column headers
     */
    setupColumnHeaders() {
        const headers = this.table.querySelectorAll('thead th');
        
        headers.forEach((header, index) => {
            const sortableColumns = ['name', 'collection', 'size', 'status', 'updated_at'];
            const columnName = this.getColumnName(header, index);
            
            if (sortableColumns.includes(columnName)) {
                header.style.cursor = 'pointer';
                header.style.userSelect = 'none';
                header.classList.add('sortable-header');
                
                // Add sort indicator container
                const indicatorContainer = document.createElement('div');
                indicatorContainer.className = 'sort-indicator-container';
                indicatorContainer.innerHTML = `
                    <span class="sort-indicator" data-column="${columnName}">
                        <i class="material-symbols-rounded sort-icon">unfold_more</i>
                        <span class="sort-priority"></span>
                    </span>
                `;
                
                header.appendChild(indicatorContainer);
                
                // Add click handler
                header.addEventListener('click', (e) => {
                    this.handleColumnClick(columnName, e);
                });
                
                // Add context menu for advanced options
                header.addEventListener('contextmenu', (e) => {
                    e.preventDefault();
                    this.showColumnContextMenu(columnName, e);
                });
            }
        });
    }

    /**
     * Get column name from header
     */
    getColumnName(header, index) {
        const columnMap = {
            0: 'name',
            1: 'collection', 
            2: 'size',
            3: 'status',
            4: 'updated_at'
        };
        
        return columnMap[index] || `column_${index}`;
    }

    /**
     * Handle column header click
     */
    handleColumnClick(columnName, event) {
        const isCtrlClick = event.ctrlKey || event.metaKey;
        const isShiftClick = event.shiftKey;
        
        if (isCtrlClick) {
            // Add to multi-column sort
            this.addSortColumn(columnName);
        } else if (isShiftClick) {
            // Remove from sort
            this.removeSortColumn(columnName);
        } else {
            // Single column sort (replace all)
            this.setSingleSort(columnName);
        }
        
        this.updateSortIndicators();
        this.applySorting();
        this.saveSortState();
    }

    /**
     * Add column to multi-column sort
     */
    addSortColumn(columnName) {
        const existingIndex = this.sortColumns.findIndex(col => col.columnName === columnName);
        
        if (existingIndex >= 0) {
            // Toggle direction
            const currentDirection = this.sortColumns[existingIndex].direction;
            this.sortColumns[existingIndex].direction = currentDirection === 'asc' ? 'desc' : 'asc';
        } else {
            // Add new sort column
            if (this.sortColumns.length >= this.maxSortColumns) {
                // Remove oldest sort column
                this.sortColumns.shift();
            }
            
            this.sortColumns.push({
                columnName: columnName,
                direction: 'asc',
                priority: this.sortColumns.length,
                displayName: this.getColumnDisplayName(columnName),
                dataType: this.getColumnDataType(columnName)
            });
        }
        
        // Update priorities
        this.updatePriorities();
    }

    /**
     * Remove column from sort
     */
    removeSortColumn(columnName) {
        this.sortColumns = this.sortColumns.filter(col => col.columnName !== columnName);
        this.updatePriorities();
    }

    /**
     * Set single column sort
     */
    setSingleSort(columnName) {
        const existingColumn = this.sortColumns.find(col => col.columnName === columnName);
        const newDirection = existingColumn ? 
            (existingColumn.direction === 'asc' ? 'desc' : 'asc') : 'asc';
        
        this.sortColumns = [{
            columnName: columnName,
            direction: newDirection,
            priority: 0,
            displayName: this.getColumnDisplayName(columnName),
            dataType: this.getColumnDataType(columnName)
        }];
    }

    /**
     * Update column priorities
     */
    updatePriorities() {
        this.sortColumns.forEach((col, index) => {
            col.priority = index;
        });
    }

    /**
     * Get column display name
     */
    getColumnDisplayName(columnName) {
        const displayNames = {
            'name': 'Document',
            'collection': 'Collection',
            'size': 'Size',
            'status': 'R2R Status',
            'updated_at': 'Modified'
        };
        
        return displayNames[columnName] || columnName;
    }

    /**
     * Get column data type
     */
    getColumnDataType(columnName) {
        const dataTypes = {
            'name': 'string',
            'collection': 'string',
            'size': 'size',
            'status': 'string',
            'updated_at': 'date'
        };
        
        return dataTypes[columnName] || 'string';
    }

    /**
     * Update visual sort indicators
     */
    updateSortIndicators() {
        // Clear all indicators
        const indicators = this.table.querySelectorAll('.sort-indicator');
        indicators.forEach(indicator => {
            const icon = indicator.querySelector('.sort-icon');
            const priority = indicator.querySelector('.sort-priority');
            
            icon.textContent = 'unfold_more';
            priority.textContent = '';
            indicator.classList.remove('sort-active', 'sort-asc', 'sort-desc');
        });
        
        // Set active indicators
        this.sortColumns.forEach((sortCol, index) => {
            const indicator = this.table.querySelector(`[data-column="${sortCol.columnName}"]`);
            if (indicator) {
                const icon = indicator.querySelector('.sort-icon');
                const priority = indicator.querySelector('.sort-priority');
                
                indicator.classList.add('sort-active');
                indicator.classList.add(`sort-${sortCol.direction}`);
                
                icon.textContent = sortCol.direction === 'asc' ? 'keyboard_arrow_up' : 'keyboard_arrow_down';
                
                if (this.sortColumns.length > 1) {
                    priority.textContent = index + 1;
                }
            }
        });
    }

    /**
     * Create sort management panel
     */
    createSortPanel() {
        const panel = document.createElement('div');
        panel.className = 'multi-sort-panel d-none';
        panel.innerHTML = `
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">
                        <i class="material-symbols-rounded me-2">sort</i>
                        Multi-Column Sorting
                    </h6>
                    <button type="button" class="btn-close" onclick="multiColumnSortManager.hideSortPanel()"></button>
                </div>
                <div class="card-body">
                    <div class="sort-columns-list" id="sortColumnsList">
                        <!-- Sort columns will be populated here -->
                    </div>
                    <div class="mt-3">
                        <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="multiColumnSortManager.addSortColumn()">
                            <i class="material-symbols-rounded me-1">add</i>
                            Add Column
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-secondary me-2" onclick="multiColumnSortManager.clearSort()">
                            <i class="material-symbols-rounded me-1">clear</i>
                            Clear All
                        </button>
                        <button type="button" class="btn btn-sm btn-primary" onclick="multiColumnSortManager.applySorting()">
                            <i class="material-symbols-rounded me-1">check</i>
                            Apply
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        // Insert panel after search form
        const searchForm = document.getElementById('documentsSearchForm');
        if (searchForm) {
            searchForm.parentNode.insertBefore(panel, searchForm.nextSibling);
        }
        
        this.sortPanel = panel;
        
        // Add toggle button to search form
        this.addSortToggleButton();
    }

    /**
     * Add sort toggle button to search form
     */
    addSortToggleButton() {
        const searchForm = document.getElementById('documentsSearchForm');
        if (searchForm) {
            const toggleButton = document.createElement('button');
            toggleButton.type = 'button';
            toggleButton.className = 'btn btn-outline-secondary btn-sm';
            toggleButton.innerHTML = '<i class="material-symbols-rounded">sort</i>';
            toggleButton.title = 'Multi-Column Sort';
            toggleButton.addEventListener('click', () => this.toggleSortPanel());
            
            const searchButton = searchForm.querySelector('button[type="submit"]');
            if (searchButton) {
                searchButton.parentNode.insertBefore(toggleButton, searchButton);
            }
        }
    }

    /**
     * Toggle sort panel visibility
     */
    toggleSortPanel() {
        if (this.sortPanel.classList.contains('d-none')) {
            this.showSortPanel();
        } else {
            this.hideSortPanel();
        }
    }

    /**
     * Show sort panel
     */
    showSortPanel() {
        this.sortPanel.classList.remove('d-none');
        this.updateSortPanel();
    }

    /**
     * Hide sort panel
     */
    hideSortPanel() {
        this.sortPanel.classList.add('d-none');
    }

    /**
     * Update sort panel content
     */
    updateSortPanel() {
        const container = document.getElementById('sortColumnsList');
        if (!container) return;
        
        if (this.sortColumns.length === 0) {
            container.innerHTML = '<p class="text-muted mb-0">No sorting applied. Click column headers or add columns below.</p>';
            return;
        }
        
        container.innerHTML = this.sortColumns.map((col, index) => `
            <div class="sort-column-item d-flex align-items-center mb-2" data-index="${index}">
                <span class="sort-priority-badge badge bg-primary me-2">${index + 1}</span>
                <div class="flex-grow-1">
                    <strong>${col.displayName}</strong>
                    <small class="text-muted ms-2">${col.direction.toUpperCase()}</small>
                </div>
                <div class="btn-group btn-group-sm">
                    <button type="button" class="btn btn-outline-secondary" onclick="multiColumnSortManager.toggleDirection(${index})" title="Toggle Direction">
                        <i class="material-symbols-rounded">${col.direction === 'asc' ? 'keyboard_arrow_up' : 'keyboard_arrow_down'}</i>
                    </button>
                    <button type="button" class="btn btn-outline-danger" onclick="multiColumnSortManager.removeSortColumnByIndex(${index})" title="Remove">
                        <i class="material-symbols-rounded">close</i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    /**
     * Apply sorting to the table
     */
    applySorting() {
        if (this.sortColumns.length === 0) return;
        
        // Build sort query string
        const sortParams = this.sortColumns.map(col => `${col.columnName}:${col.direction}`).join(',');
        
        // Update form and submit
        const form = document.getElementById('documentsSearchForm');
        if (form) {
            // Update or create hidden input for multi-sort
            let sortInput = form.querySelector('input[name="MultiSort"]');
            if (!sortInput) {
                sortInput = document.createElement('input');
                sortInput.type = 'hidden';
                sortInput.name = 'MultiSort';
                form.appendChild(sortInput);
            }
            sortInput.value = sortParams;
            
            // Submit form
            form.submit();
        }
    }

    /**
     * Clear all sorting
     */
    clearSort() {
        this.sortColumns = [];
        this.updateSortIndicators();
        this.updateSortPanel();
        this.applySorting();
        this.saveSortState();
    }

    /**
     * Save sort state to localStorage
     */
    saveSortState() {
        localStorage.setItem('documentsSortState', JSON.stringify(this.sortColumns));
    }

    /**
     * Load sort state from localStorage
     */
    loadSortState() {
        const saved = localStorage.getItem('documentsSortState');
        if (saved) {
            try {
                this.sortColumns = JSON.parse(saved);
            } catch (e) {
                console.warn('Failed to load sort state:', e);
                this.sortColumns = [];
            }
        }
    }

    /**
     * Show column context menu
     */
    showColumnContextMenu(columnName, event) {
        // Implementation for context menu
        console.log('Context menu for column:', columnName);
    }

    /**
     * Toggle sort direction for column at index
     */
    toggleDirection(index) {
        if (this.sortColumns[index]) {
            this.sortColumns[index].direction = this.sortColumns[index].direction === 'asc' ? 'desc' : 'asc';
            this.updateSortIndicators();
            this.updateSortPanel();
        }
    }

    /**
     * Remove sort column by index
     */
    removeSortColumnByIndex(index) {
        this.sortColumns.splice(index, 1);
        this.updatePriorities();
        this.updateSortIndicators();
        this.updateSortPanel();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.multiColumnSortManager = new MultiColumnSortManager();
});
