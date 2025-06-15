/**
 * Chart Manager for Hybrid.CleverDocs2
 * Manages Chart.js instances with Material Dashboard integration
 */

// Global chart instances storage
window.chartInstances = window.chartInstances || {};

/**
 * Initialize a chart with given configuration
 * @param {string} chartId - Canvas element ID
 * @param {object} config - Chart configuration
 */
window.initializeChart = async function(chartId, config) {
    try {
        // Show loading state
        showChartLoading(chartId, true);
        hideChartError(chartId);

        // Fetch chart data from API
        const chartData = await fetchChartData(config.dataUrl);
        
        // Parse chart configuration
        const chartConfig = JSON.parse(chartData);
        
        // Apply custom options if provided
        if (config.customOptions) {
            chartConfig.options = { ...chartConfig.options, ...config.customOptions };
        }

        // Apply theme
        applyChartTheme(chartConfig, config.theme);

        // Apply animation settings
        if (chartConfig.options) {
            chartConfig.options.animation = {
                duration: config.animationDuration || 1000,
                easing: 'easeOutQuart'
            };
        }

        // Add click handler if enabled
        if (config.enableClick && config.clickHandler) {
            chartConfig.options.onClick = function(event, elements) {
                if (elements.length > 0 && window[config.clickHandler]) {
                    window[config.clickHandler](event, elements, this);
                }
            };
        }

        // Create chart instance
        const canvas = document.getElementById(chartId);
        const ctx = canvas.getContext('2d');
        
        // Destroy existing chart if it exists
        if (window.chartInstances[chartId]) {
            window.chartInstances[chartId].destroy();
        }

        // Create new chart
        window.chartInstances[chartId] = new Chart(ctx, chartConfig);

        // Hide loading and show chart
        showChartLoading(chartId, false);
        canvas.style.display = 'block';

        // Set up auto-refresh if configured
        if (config.refreshInterval > 0) {
            setChartRefreshInterval(chartId, config);
        }

        console.log(`Chart ${chartId} initialized successfully`);

    } catch (error) {
        console.error(`Error initializing chart ${chartId}:`, error);
        showChartError(chartId, error.message);
        showChartLoading(chartId, false);
    }
};

/**
 * Fetch chart data from API endpoint
 * @param {string} dataUrl - API endpoint URL
 * @returns {Promise<string>} Chart configuration JSON
 */
async function fetchChartData(dataUrl) {
    const response = await fetch(dataUrl, {
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

    return await response.text();
}

/**
 * Apply theme to chart configuration
 * @param {object} chartConfig - Chart.js configuration
 * @param {string} theme - Theme name (light, dark, auto)
 */
function applyChartTheme(chartConfig, theme) {
    const isDark = theme === 'dark' || 
                  (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches);

    if (isDark) {
        // Apply dark theme colors
        if (chartConfig.options && chartConfig.options.scales) {
            Object.keys(chartConfig.options.scales).forEach(scaleKey => {
                const scale = chartConfig.options.scales[scaleKey];
                if (scale.grid) {
                    scale.grid.color = 'rgba(255, 255, 255, 0.1)';
                }
                if (scale.ticks) {
                    scale.ticks.color = '#ffffff';
                }
            });
        }

        if (chartConfig.options && chartConfig.options.plugins && chartConfig.options.plugins.legend) {
            chartConfig.options.plugins.legend.labels = {
                ...chartConfig.options.plugins.legend.labels,
                color: '#ffffff'
            };
        }
    }
}

/**
 * Show/hide chart loading state
 * @param {string} chartId - Chart ID
 * @param {boolean} show - Show loading state
 */
function showChartLoading(chartId, show) {
    const loadingElement = document.getElementById(`${chartId}-loading`);
    if (loadingElement) {
        loadingElement.style.display = show ? 'block' : 'none';
    }
}

/**
 * Show chart error state
 * @param {string} chartId - Chart ID
 * @param {string} message - Error message
 */
function showChartError(chartId, message) {
    const errorElement = document.getElementById(`${chartId}-error`);
    if (errorElement) {
        errorElement.classList.remove('d-none');
        const messageElement = errorElement.querySelector('p');
        if (messageElement && message) {
            messageElement.textContent = message;
        }
    }
}

/**
 * Hide chart error state
 * @param {string} chartId - Chart ID
 */
function hideChartError(chartId) {
    const errorElement = document.getElementById(`${chartId}-error`);
    if (errorElement) {
        errorElement.classList.add('d-none');
    }
}

/**
 * Refresh chart data
 * @param {string} chartId - Chart ID
 */
window.refreshChart = async function(chartId) {
    const chartCard = document.querySelector(`[data-chart-id="${chartId}"]`);
    if (!chartCard) return;

    // Get original configuration from data attributes or stored config
    const config = getStoredChartConfig(chartId);
    if (config) {
        await initializeChart(chartId, config);
    }
};

/**
 * Export chart as image or PDF
 * @param {string} chartId - Chart ID
 * @param {string} format - Export format (png, pdf)
 */
window.exportChart = function(chartId, format) {
    const chart = window.chartInstances[chartId];
    if (!chart) return;

    try {
        if (format === 'png') {
            const url = chart.toBase64Image();
            const link = document.createElement('a');
            link.download = `chart-${chartId}.png`;
            link.href = url;
            link.click();
        } else if (format === 'pdf') {
            // For PDF export, you might want to use a library like jsPDF
            console.log('PDF export not implemented yet');
        }
    } catch (error) {
        console.error('Error exporting chart:', error);
    }
};

/**
 * Set up auto-refresh interval for chart
 * @param {string} chartId - Chart ID
 * @param {object} config - Chart configuration
 */
function setChartRefreshInterval(chartId, config) {
    // Clear existing interval
    if (window.chartIntervals && window.chartIntervals[chartId]) {
        clearInterval(window.chartIntervals[chartId]);
    }

    // Initialize intervals storage
    window.chartIntervals = window.chartIntervals || {};

    // Set new interval
    window.chartIntervals[chartId] = setInterval(() => {
        refreshChart(chartId);
    }, config.refreshInterval * 1000);
}

/**
 * Store chart configuration for later use
 * @param {string} chartId - Chart ID
 * @param {object} config - Chart configuration
 */
function storeChartConfig(chartId, config) {
    window.chartConfigs = window.chartConfigs || {};
    window.chartConfigs[chartId] = config;
}

/**
 * Get stored chart configuration
 * @param {string} chartId - Chart ID
 * @returns {object|null} Chart configuration
 */
function getStoredChartConfig(chartId) {
    return window.chartConfigs && window.chartConfigs[chartId] || null;
}

/**
 * Destroy all charts (cleanup function)
 */
window.destroyAllCharts = function() {
    Object.keys(window.chartInstances || {}).forEach(chartId => {
        if (window.chartInstances[chartId]) {
            window.chartInstances[chartId].destroy();
            delete window.chartInstances[chartId];
        }
    });

    Object.keys(window.chartIntervals || {}).forEach(chartId => {
        clearInterval(window.chartIntervals[chartId]);
        delete window.chartIntervals[chartId];
    });
};

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    destroyAllCharts();
});
