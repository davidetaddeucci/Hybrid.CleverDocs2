/**
 * Material Dashboard Extensions
 * Namespace: MaterialDashboardExt
 */
window.MaterialDashboardExt = (function() {
    'use strict';

    // Private variables
    let isCollapsed = false;
    let isMobile = window.innerWidth < 1200;
    
    // Configuration
    const config = {
        storageKey: 'md-ext-sidebar-state',
        animationDuration: 300,
        searchDelay: 300
    };

    // DOM elements
    const elements = {
        sidebar: null,
        toggleBtn: null,
        overlay: null,
        searchInput: null,
        submenuItems: null
    };

    /**
     * Initialize the extension
     */
    function init() {
        // Cache DOM elements
        elements.sidebar = document.getElementById('sidenav-main');
        elements.toggleBtn = document.getElementById('sidenavToggle');
        elements.overlay = document.getElementById('sidenavOverlay');
        elements.searchInput = document.querySelector('.md-ext-search-input');
        elements.submenuItems = document.querySelectorAll('.md-ext-submenu');

        if (!elements.sidebar) {
            console.warn('MaterialDashboardExt: Sidebar not found');
            return;
        }

        // Load saved state
        loadSavedState();
        
        // Bind events
        bindEvents();
        
        // Initialize responsive behavior
        handleResize();
        
        console.log('MaterialDashboardExt: Initialized successfully');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        // Toggle button
        if (elements.toggleBtn) {
            elements.toggleBtn.addEventListener('click', toggleSidebar);
        }

        // Overlay click (mobile)
        if (elements.overlay) {
            elements.overlay.addEventListener('click', closeSidebar);
        }

        // Submenu toggles - Enhanced Bootstrap collapse integration
        elements.submenuItems.forEach(item => {
            const link = item.querySelector('a[data-bs-toggle="collapse"]');
            if (link) {
                const targetId = link.getAttribute('data-bs-target');
                const targetElement = document.querySelector(targetId);

                if (targetElement) {
                    // Initialize Bootstrap Collapse
                    const bsCollapse = new bootstrap.Collapse(targetElement, {
                        toggle: false
                    });

                    // Manual click handler for better control
                    link.addEventListener('click', function(e) {
                        e.preventDefault();

                        // Close other submenus (accordion behavior)
                        elements.submenuItems.forEach(otherItem => {
                            if (otherItem !== item) {
                                const otherLink = otherItem.querySelector('a[data-bs-toggle="collapse"]');
                                const otherTargetId = otherLink?.getAttribute('data-bs-target');
                                const otherTarget = otherTargetId ? document.querySelector(otherTargetId) : null;

                                if (otherTarget && otherTarget.classList.contains('show')) {
                                    const otherCollapse = bootstrap.Collapse.getInstance(otherTarget);
                                    if (otherCollapse) {
                                        otherCollapse.hide();
                                    }
                                }
                            }
                        });

                        // Toggle current submenu
                        bsCollapse.toggle();
                    });

                    // Listen to Bootstrap collapse events
                    targetElement.addEventListener('show.bs.collapse', function() {
                        item.classList.add('md-ext-expanded');
                        link.setAttribute('aria-expanded', 'true');
                        console.log('Submenu expanded:', item);
                    });

                    targetElement.addEventListener('hide.bs.collapse', function() {
                        item.classList.remove('md-ext-expanded');
                        link.setAttribute('aria-expanded', 'false');
                        console.log('Submenu collapsed:', item);
                    });
                }
            }
        });

        // Search functionality
        if (elements.searchInput) {
            let searchTimeout;
            elements.searchInput.addEventListener('input', function(e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    handleSearch(e.target.value);
                }, config.searchDelay);
            });
        }

        // Window resize
        window.addEventListener('resize', debounce(handleResize, 250));

        // Keyboard navigation
        document.addEventListener('keydown', handleKeyboard);
    }

    /**
     * Toggle sidebar state
     */
    function toggleSidebar() {
        if (isMobile) {
            toggleMobileSidebar();
        } else {
            toggleDesktopSidebar();
        }
    }

    /**
     * Toggle desktop sidebar (collapse/expand)
     */
    function toggleDesktopSidebar() {
        isCollapsed = !isCollapsed;
        
        elements.sidebar.classList.add('md-ext-animating');
        
        if (isCollapsed) {
            elements.sidebar.classList.add('md-ext-collapsed');
            elements.toggleBtn.setAttribute('aria-expanded', 'false');
        } else {
            elements.sidebar.classList.remove('md-ext-collapsed');
            elements.toggleBtn.setAttribute('aria-expanded', 'true');
        }

        // Save state
        saveState();

        // Remove animation class after transition
        setTimeout(() => {
            elements.sidebar.classList.remove('md-ext-animating');
        }, config.animationDuration);
    }

    /**
     * Toggle mobile sidebar (show/hide)
     */
    function toggleMobileSidebar() {
        const isVisible = elements.sidebar.classList.contains('md-ext-show');
        
        if (isVisible) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }

    /**
     * Open sidebar (mobile)
     */
    function openSidebar() {
        elements.sidebar.classList.add('md-ext-show');
        elements.overlay.classList.add('md-ext-show');
        elements.toggleBtn.setAttribute('aria-expanded', 'true');
        
        // Prevent body scroll
        document.body.style.overflow = 'hidden';
    }

    /**
     * Close sidebar (mobile)
     */
    function closeSidebar() {
        elements.sidebar.classList.remove('md-ext-show');
        elements.overlay.classList.remove('md-ext-show');
        elements.toggleBtn.setAttribute('aria-expanded', 'false');
        
        // Restore body scroll
        document.body.style.overflow = '';
    }

    // handleSubmenuToggle function removed - now using Bootstrap collapse events

    /**
     * Handle search functionality
     */
    function handleSearch(query) {
        const navItems = document.querySelectorAll('.navbar-nav .nav-item');
        
        if (!query.trim()) {
            // Show all items
            navItems.forEach(item => {
                item.style.display = '';
            });
            return;
        }
        
        const searchTerm = query.toLowerCase();
        
        navItems.forEach(item => {
            const text = item.textContent.toLowerCase();
            const shouldShow = text.includes(searchTerm);
            
            item.style.display = shouldShow ? '' : 'none';
        });
    }

    /**
     * Handle window resize
     */
    function handleResize() {
        const wasMobile = isMobile;
        isMobile = window.innerWidth < 1200;
        
        if (wasMobile !== isMobile) {
            // Reset states when switching between mobile/desktop
            if (isMobile) {
                elements.sidebar.classList.remove('md-ext-collapsed');
                closeSidebar();
            } else {
                elements.sidebar.classList.remove('md-ext-show');
                elements.overlay.classList.remove('md-ext-show');
                document.body.style.overflow = '';
                
                // Restore collapsed state on desktop
                if (isCollapsed) {
                    elements.sidebar.classList.add('md-ext-collapsed');
                }
            }
        }
    }

    /**
     * Handle keyboard navigation
     */
    function handleKeyboard(e) {
        // ESC key closes mobile sidebar
        if (e.key === 'Escape' && isMobile) {
            closeSidebar();
        }
        
        // Toggle with Ctrl+B
        if (e.ctrlKey && e.key === 'b') {
            e.preventDefault();
            toggleSidebar();
        }
    }

    /**
     * Save sidebar state to localStorage
     */
    function saveState() {
        try {
            localStorage.setItem(config.storageKey, JSON.stringify({
                isCollapsed: isCollapsed,
                timestamp: Date.now()
            }));
        } catch (e) {
            console.warn('MaterialDashboardExt: Could not save state to localStorage');
        }
    }

    /**
     * Load saved state from localStorage
     */
    function loadSavedState() {
        try {
            const saved = localStorage.getItem(config.storageKey);
            if (saved) {
                const state = JSON.parse(saved);
                
                // Only apply saved state if not mobile and not too old (24h)
                if (!isMobile && (Date.now() - state.timestamp) < 86400000) {
                    isCollapsed = state.isCollapsed;
                    
                    if (isCollapsed) {
                        elements.sidebar.classList.add('md-ext-collapsed');
                        if (elements.toggleBtn) {
                            elements.toggleBtn.setAttribute('aria-expanded', 'false');
                        }
                    }
                }
            }
        } catch (e) {
            console.warn('MaterialDashboardExt: Could not load state from localStorage');
        }
    }

    /**
     * Utility: Debounce function
     */
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

    // Public API
    return {
        init: init,
        toggle: toggleSidebar,
        collapse: () => {
            if (!isMobile && !isCollapsed) toggleDesktopSidebar();
        },
        expand: () => {
            if (!isMobile && isCollapsed) toggleDesktopSidebar();
        },
        isCollapsed: () => isCollapsed,
        isMobile: () => isMobile
    };
})();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    MaterialDashboardExt.init();
});
