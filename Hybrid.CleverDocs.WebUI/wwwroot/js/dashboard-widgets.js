/**
 * Dashboard Widgets Manager for Hybrid.CleverDocs2
 * Handles drag-and-drop widget positioning with SortableJS
 */

// Global widget manager
window.DashboardWidgets = (function() {
    'use strict';

    let widgets = [];
    let sortableInstance = null;
    let gridContainer = null;
    let isEditMode = false;

    /**
     * Initialize dashboard widgets system
     */
    function init() {
        gridContainer = document.getElementById('dashboard-grid');
        if (!gridContainer) {
            console.warn('Dashboard grid container not found');
            return;
        }

        loadWidgets();
        setupEventListeners();
        
        console.log('Dashboard Widgets initialized');
    }

    /**
     * Load widgets from API
     */
    async function loadWidgets() {
        try {
            showLoading(true);
            
            const response = await fetch('/api/widgets/dashboard', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            widgets = await response.json();
            renderWidgets();
            
        } catch (error) {
            console.error('Error loading widgets:', error);
            showError('Failed to load dashboard widgets');
        } finally {
            showLoading(false);
        }
    }

    /**
     * Render widgets in the grid
     */
    function renderWidgets() {
        if (!gridContainer) return;

        // Clear existing widgets
        gridContainer.innerHTML = '';

        // Sort widgets by position
        const sortedWidgets = widgets
            .filter(w => w.isVisible)
            .sort((a, b) => (a.y * 12 + a.x) - (b.y * 12 + b.x));

        // Render each widget
        sortedWidgets.forEach(widget => {
            const widgetElement = createWidgetElement(widget);
            gridContainer.appendChild(widgetElement);
        });

        // Initialize sortable if in edit mode
        if (isEditMode) {
            initializeSortable();
        }
    }

    /**
     * Create widget DOM element
     */
    function createWidgetElement(widget) {
        const widgetDiv = document.createElement('div');
        widgetDiv.className = `dashboard-widget col-lg-${widget.width} col-md-${Math.min(widget.width + 2, 12)} col-12`;
        widgetDiv.setAttribute('data-widget-id', widget.id);
        widgetDiv.setAttribute('data-widget-type', widget.type);
        widgetDiv.style.order = widget.order || 0;

        // Add edit mode controls
        if (isEditMode) {
            widgetDiv.classList.add('widget-editable');
            widgetDiv.innerHTML = `
                <div class="widget-controls">
                    <button class="btn btn-sm btn-outline-secondary widget-drag-handle" title="Drag to reorder">
                        <i class="material-symbols-rounded">drag_indicator</i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger widget-hide" title="Hide widget" onclick="hideWidget('${widget.id}')">
                        <i class="material-symbols-rounded">visibility_off</i>
                    </button>
                </div>
                <div class="widget-content">
                    ${getWidgetContent(widget)}
                </div>
            `;
        } else {
            widgetDiv.innerHTML = `
                <div class="widget-content">
                    ${getWidgetContent(widget)}
                </div>
            `;
        }

        return widgetDiv;
    }

    /**
     * Get widget content based on type
     */
    function getWidgetContent(widget) {
        const config = JSON.parse(widget.configuration || '{}');
        
        switch (widget.type) {
            case 'StatCard':
                return createStatCardContent(widget, config);
            case 'Chart':
                return createChartContent(widget, config);
            default:
                return `<div class="card"><div class="card-body">Unknown widget type: ${widget.type}</div></div>`;
        }
    }

    /**
     * Create StatCard content
     */
    function createStatCardContent(widget, config) {
        const cardId = `stat-card-${widget.id}`;
        return `
            <div class="card stat-card" id="${cardId}">
                <div class="card-header p-2 ps-3">
                    <div class="d-flex justify-content-between">
                        <div>
                            <p class="text-sm mb-0 text-capitalize text-white opacity-7">${widget.title}</p>
                            <h4 class="font-weight-bolder mb-0 text-white" data-stat-value="0" data-animate="true">
                                <span class="stat-counter">0</span>
                            </h4>
                        </div>
                        <div class="icon icon-md icon-shape bg-gradient-${config.color || 'primary'} shadow-primary text-center border-radius-md">
                            <i class="material-symbols-rounded opacity-10">${config.icon || 'analytics'}</i>
                        </div>
                    </div>
                </div>
                <div class="card-body p-2 ps-3">
                    <p class="mb-0 text-sm">
                        <span class="text-success font-weight-bolder">
                            <i class="material-symbols-rounded text-xs">trending_up</i>
                            ${config.trend || '+0%'}
                        </span>
                        vs last month
                    </p>
                </div>
            </div>
        `;
    }

    /**
     * Create Chart content
     */
    function createChartContent(widget, config) {
        const chartId = `chart-${widget.id}`;
        return `
            <div class="card chart-card">
                <div class="card-header pb-0">
                    <h6 class="mb-0">${widget.title}</h6>
                </div>
                <div class="card-body">
                    <div class="chart-container" style="height: 300px; position: relative;">
                        <div class="chart-loading" id="${chartId}-loading">
                            <div class="d-flex justify-content-center align-items-center h-100">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Loading chart...</span>
                                </div>
                            </div>
                        </div>
                        <canvas id="${chartId}" class="chart-canvas" style="display: none;"></canvas>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Initialize SortableJS for drag-and-drop
     */
    function initializeSortable() {
        if (sortableInstance) {
            sortableInstance.destroy();
        }

        sortableInstance = new Sortable(gridContainer, {
            handle: '.widget-drag-handle',
            animation: 150,
            ghostClass: 'widget-ghost',
            chosenClass: 'widget-chosen',
            dragClass: 'widget-drag',
            onEnd: function(evt) {
                handleWidgetReorder(evt);
            }
        });
    }

    /**
     * Handle widget reorder after drag-and-drop
     */
    function handleWidgetReorder(evt) {
        const widgetId = evt.item.getAttribute('data-widget-id');
        const newIndex = evt.newIndex;
        const oldIndex = evt.oldIndex;

        console.log(`Widget ${widgetId} moved from ${oldIndex} to ${newIndex}`);

        // Update widget order in memory
        const widget = widgets.find(w => w.id === widgetId);
        if (widget) {
            widget.order = newIndex;
        }

        // Save changes
        saveWidgetConfiguration();
    }

    /**
     * Toggle edit mode
     */
    function toggleEditMode() {
        isEditMode = !isEditMode;
        
        const editButton = document.getElementById('dashboard-edit-btn');
        const saveButton = document.getElementById('dashboard-save-btn');
        const cancelButton = document.getElementById('dashboard-cancel-btn');

        if (isEditMode) {
            editButton.style.display = 'none';
            saveButton.style.display = 'inline-block';
            cancelButton.style.display = 'inline-block';
            gridContainer.classList.add('edit-mode');
        } else {
            editButton.style.display = 'inline-block';
            saveButton.style.display = 'none';
            cancelButton.style.display = 'none';
            gridContainer.classList.remove('edit-mode');
            
            if (sortableInstance) {
                sortableInstance.destroy();
                sortableInstance = null;
            }
        }

        renderWidgets();
    }

    /**
     * Save widget configuration
     */
    async function saveWidgetConfiguration() {
        try {
            const response = await fetch('/api/widgets/dashboard', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin',
                body: JSON.stringify(widgets)
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('Widgets saved:', result);
            
            showSuccess('Dashboard layout saved successfully');
            
        } catch (error) {
            console.error('Error saving widgets:', error);
            showError('Failed to save dashboard layout');
        }
    }

    /**
     * Hide widget
     */
    function hideWidget(widgetId) {
        const widget = widgets.find(w => w.id === widgetId);
        if (widget) {
            widget.isVisible = false;
            renderWidgets();
            saveWidgetConfiguration();
        }
    }

    /**
     * Setup event listeners
     */
    function setupEventListeners() {
        // Edit mode buttons
        const editButton = document.getElementById('dashboard-edit-btn');
        const saveButton = document.getElementById('dashboard-save-btn');
        const cancelButton = document.getElementById('dashboard-cancel-btn');

        if (editButton) {
            editButton.addEventListener('click', toggleEditMode);
        }

        if (saveButton) {
            saveButton.addEventListener('click', function() {
                saveWidgetConfiguration();
                toggleEditMode();
            });
        }

        if (cancelButton) {
            cancelButton.addEventListener('click', function() {
                loadWidgets(); // Reload original configuration
                toggleEditMode();
            });
        }
    }

    /**
     * Show loading state
     */
    function showLoading(show) {
        const loadingElement = document.getElementById('dashboard-loading');
        if (loadingElement) {
            loadingElement.style.display = show ? 'block' : 'none';
        }
    }

    /**
     * Show error message
     */
    function showError(message) {
        console.error(message);
        // TODO: Implement toast notification
    }

    /**
     * Show success message
     */
    function showSuccess(message) {
        console.log(message);
        // TODO: Implement toast notification
    }

    // Public API
    return {
        init: init,
        toggleEditMode: toggleEditMode,
        hideWidget: hideWidget,
        loadWidgets: loadWidgets,
        saveConfiguration: saveWidgetConfiguration
    };

})();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Load SortableJS from CDN if not already loaded
    if (typeof Sortable === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/sortablejs@1.15.0/Sortable.min.js';
        script.onload = function() {
            DashboardWidgets.init();
        };
        document.head.appendChild(script);
    } else {
        DashboardWidgets.init();
    }
});

// Global functions for widget actions
window.hideWidget = function(widgetId) {
    DashboardWidgets.hideWidget(widgetId);
};
