// Wagl Backend API Demo - UI Components

// Page Manager
class PageManager {
    constructor() {
        this.currentPage = 'auth';
        this.pages = new Map();
        this.initializePages();
    }

    initializePages() {
        // Register all pages
        this.pages.set('auth', DOMUtils.$('#auth-page'));
        this.pages.set('dashboard', DOMUtils.$('#dashboard-page'));
        this.pages.set('anonymous-chat', DOMUtils.$('#anonymous-chat-page'));
        this.pages.set('moderator-chat', DOMUtils.$('#moderator-chat-page'));
        this.pages.set('public-dashboard', DOMUtils.$('#public-dashboard-page'));
    }

    showPage(pageId) {
        // Hide all pages
        this.pages.forEach((element, id) => {
            if (element) {
                DOMUtils.hide(element);
            }
        });

        // Show target page
        const targetPage = this.pages.get(pageId);
        if (targetPage) {
            DOMUtils.show(targetPage);
            this.currentPage = pageId;
            window.APP_STATE.ui.currentPage = pageId;

            debugLog('Page changed to:', pageId);

            // Page-specific initialization
            this.onPageShow(pageId);
        } else {
            errorLog(new Error('Page not found'), `PageManager.showPage(${pageId})`);
        }
    }

    onPageShow(pageId) {
        switch (pageId) {
            case 'dashboard':
                if (window.dashboardManager) {
                    window.dashboardManager.refreshData();
                }
                break;
            case 'anonymous-chat':
                if (window.chatManager) {
                    window.chatManager.initializeAnonymousChat();
                }
                break;
            case 'public-dashboard':
                if (window.publicDashboardManager) {
                    window.publicDashboardManager.refreshData();
                }
                break;
            case 'moderator-chat':
                if (window.chatManager) {
                    window.chatManager.initializeModeratorChat();
                }
                break;
        }
    }

    getCurrentPage() {
        return this.currentPage;
    }
}

// Tab Manager
class TabManager {
    constructor() {
        this.setupTabHandlers();
    }

    setupTabHandlers() {
        // Handle tab clicks
        DOMUtils.$$('.tab-btn').forEach(tabBtn => {
            DOMUtils.on(tabBtn, 'click', (e) => {
                e.preventDefault();
                const tabId = tabBtn.getAttribute('data-tab');
                this.showTab(tabBtn, tabId);
            });
        });
    }

    showTab(clickedTab, tabId) {
        const tabContainer = clickedTab.closest('.tab-container, .dashboard-tabs');
        if (!tabContainer) return;

        // Remove active class from all tabs in this container
        tabContainer.querySelectorAll('.tab-btn').forEach(tab => {
            tab.classList.remove('active');
        });

        // Remove active class from all tab contents in this container
        tabContainer.querySelectorAll('.tab-content').forEach(content => {
            content.classList.remove('active');
        });

        // Add active class to clicked tab
        clickedTab.classList.add('active');

        // Show corresponding tab content
        const tabContent = tabContainer.querySelector(`#${tabId}-tab`);
        if (tabContent) {
            tabContent.classList.add('active');
        }

        // Update app state
        window.APP_STATE.ui.currentTab = tabId;

        debugLog('Tab changed to:', tabId);
    }
}

// Modal Manager
class ModalManager {
    constructor() {
        this.currentModal = null;
        this.setupModalHandlers();
    }

    setupModalHandlers() {
        const overlay = DOMUtils.$('#modal-overlay');
        if (!overlay) return;

        // Close modal when clicking overlay
        DOMUtils.on(overlay, 'click', (e) => {
            if (e.target === overlay) {
                this.closeModal();
            }
        });

        // Close modal when clicking close buttons
        DOMUtils.$$('.modal-close').forEach(closeBtn => {
            DOMUtils.on(closeBtn, 'click', () => {
                this.closeModal();
            });
        });

        // Close modal with Escape key
        DOMUtils.on(document, 'keydown', (e) => {
            if (e.key === 'Escape' && this.currentModal) {
                this.closeModal();
            }
        });
    }

    showModal(modalId) {
        const overlay = DOMUtils.$('#modal-overlay');
        const modal = DOMUtils.$(`#${modalId}`);

        if (!overlay || !modal) {
            errorLog(new Error('Modal not found'), `ModalManager.showModal(${modalId})`);
            return;
        }

        // Hide all modals
        DOMUtils.$$('.modal').forEach(m => DOMUtils.hide(m));

        // Show target modal
        DOMUtils.show(modal);
        DOMUtils.show(overlay);

        this.currentModal = modalId;

        // Focus first input in modal
        const firstInput = modal.querySelector('input, select, textarea');
        if (firstInput) {
            setTimeout(() => firstInput.focus(), 100);
        }

        debugLog('Modal opened:', modalId);
    }

    closeModal() {
        const overlay = DOMUtils.$('#modal-overlay');
        if (overlay) {
            DOMUtils.hide(overlay);
        }

        // Hide all modals
        DOMUtils.$$('.modal').forEach(modal => DOMUtils.hide(modal));

        this.currentModal = null;

        debugLog('Modal closed');
    }

    isModalOpen() {
        return this.currentModal !== null;
    }
}

// Toast Manager
class ToastManager {
    static toastContainer = null;
    static toastId = 0;

    static initialize() {
        this.toastContainer = DOMUtils.$('#toast-container');
        if (!this.toastContainer) {
            // Create toast container if it doesn't exist
            this.toastContainer = DOMUtils.create('div', {
                id: 'toast-container',
                class: 'toast-container'
            });
            document.body.appendChild(this.toastContainer);
        }
    }

    static show(type, title, message, duration = CONFIG.UI.TOAST_DURATION) {
        if (!this.toastContainer) this.initialize();

        const toastId = `toast-${++this.toastId}`;
        const toast = DOMUtils.create('div', {
            id: toastId,
            class: `toast ${type}`
        });

        const header = DOMUtils.create('div', { class: 'toast-header' });
        const titleElement = DOMUtils.create('div', { class: 'toast-title' }, title);
        const closeBtn = DOMUtils.create('button', { class: 'toast-close' }, '×');

        header.appendChild(titleElement);
        header.appendChild(closeBtn);

        const messageElement = DOMUtils.create('div', { class: 'toast-message' }, message);

        toast.appendChild(header);
        toast.appendChild(messageElement);

        // Add close handler
        DOMUtils.on(closeBtn, 'click', () => this.remove(toastId));

        // Add to container
        this.toastContainer.appendChild(toast);

        // Auto-remove after duration
        if (duration > 0) {
            setTimeout(() => this.remove(toastId), duration);
        }

        debugLog('Toast shown:', { type, title, message });

        return toastId;
    }

    static remove(toastId) {
        const toast = DOMUtils.$(`#${toastId}`);
        if (toast) {
            toast.classList.add('removing');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }
    }

    static showSuccess(title, message, duration) {
        return this.show('success', title, message, duration);
    }

    static showError(title, message, duration) {
        return this.show('error', title, message, duration);
    }

    static showWarning(title, message, duration) {
        return this.show('warning', title, message, duration);
    }

    static showInfo(title, message, duration) {
        return this.show('info', title, message, duration);
    }
}

// Form Manager
class FormManager {
    static setupForm(formId, submitHandler, validationRules = {}) {
        const form = DOMUtils.$(formId);
        if (!form) return;

        DOMUtils.on(form, 'submit', async (e) => {
            e.preventDefault();

            const formData = new FormData(form);
            const data = Object.fromEntries(formData);

            // Apply transformations
            Object.keys(data).forEach(key => {
                const input = form.querySelector(`[name="${key}"]`);
                if (input) {
                    // Convert numbers
                    if (input.type === 'number') {
                        data[key] = parseFloat(data[key]) || 0;
                    }
                    // Convert checkboxes
                    if (input.type === 'checkbox') {
                        data[key] = input.checked;
                    }
                    // Convert datetime-local to ISO string
                    if (input.type === 'datetime-local') {
                        data[key] = new Date(data[key]).toISOString();
                    }
                }
            });

            // Skip validation for demo

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            const originalText = submitBtn?.textContent;
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = 'Processing...';
            }

            try {
                await submitHandler(data);
                form.reset(); // Clear form on success
            } catch (error) {
                errorLog(error, `Form submission: ${formId}`);
            } finally {
                // Restore button state
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }
            }
        });

        debugLog('Form setup completed:', formId);
    }

    static validateForm(data, rules) {
        // Skip validation for demo - just return valid
        return { isValid: true };
    }
}

// Loading Manager
class LoadingManager {
    static show(elementId) {
        const element = typeof elementId === 'string' ? DOMUtils.$(elementId) : elementId;
        if (element) {
            element.classList.add('loading');
            window.APP_STATE.ui.isLoading = true;
        }
    }

    static hide(elementId) {
        const element = typeof elementId === 'string' ? DOMUtils.$(elementId) : elementId;
        if (element) {
            element.classList.remove('loading');
            window.APP_STATE.ui.isLoading = false;
        }
    }

    static showGlobal() {
        document.body.classList.add('loading');
        window.APP_STATE.ui.isLoading = true;
    }

    static hideGlobal() {
        document.body.classList.remove('loading');
        window.APP_STATE.ui.isLoading = false;
    }
}

// Copy to Clipboard Component
class CopyComponent {
    static setup(containerId, text, successMessage = 'Copied to clipboard!') {
        const container = DOMUtils.$(containerId);
        if (!container) return;

        const input = container.querySelector('.copy-input');
        const button = container.querySelector('.copy-btn');

        if (input) {
            input.value = text;
            input.readOnly = true;
        }

        if (button) {
            DOMUtils.on(button, 'click', async () => {
                const success = await HttpUtils.copyToClipboard(text);

                if (success) {
                    button.classList.add('copied');
                    button.innerHTML = '✓';
                    ToastManager.showSuccess('Copied!', successMessage);

                    setTimeout(() => {
                        button.classList.remove('copied');
                        button.innerHTML = '📋';
                    }, 2000);
                } else {
                    ToastManager.showError('Copy Failed', 'Unable to copy to clipboard');
                }
            });
        }
    }
}

// Data List Component
class DataListManager {
    static renderList(containerId, items, renderItem) {
        const container = DOMUtils.$(containerId);
        if (!container) return;

        // Clear existing content
        container.innerHTML = '';

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-state-icon">📭</div>
                    <h3>No items found</h3>
                    <p>There are no items to display at this time.</p>
                </div>
            `;
            return;
        }

        // Render items
        items.forEach((item, index) => {
            const itemElement = renderItem(item, index);
            if (itemElement) {
                container.appendChild(itemElement);
            }
        });
    }

    static createCard(title, content, actions = []) {
        const card = DOMUtils.create('div', { class: 'card' });

        if (title) {
            const header = DOMUtils.create('div', { class: 'card-header' });
            const titleElement = DOMUtils.create('h4', {}, title);
            header.appendChild(titleElement);

            if (actions.length > 0) {
                const actionsContainer = DOMUtils.create('div', { class: 'action-group' });
                actions.forEach(action => {
                    const button = DOMUtils.create('button', {
                        class: `btn ${action.class || 'btn-outline'}`
                    }, action.text);

                    if (action.onClick) {
                        DOMUtils.on(button, 'click', action.onClick);
                    }

                    actionsContainer.appendChild(button);
                });
                header.appendChild(actionsContainer);
            }

            card.appendChild(header);
        }

        if (content) {
            const body = DOMUtils.create('div', { class: 'card-body' }, content);
            card.appendChild(body);
        }

        return card;
    }
}

// Export managers
window.pageManager = new PageManager();
window.tabManager = new TabManager();
window.modalManager = new ModalManager();
window.ToastManager = ToastManager;
window.FormManager = FormManager;
window.LoadingManager = LoadingManager;
window.CopyComponent = CopyComponent;
window.DataListManager = DataListManager;