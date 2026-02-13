/**
 * Toast Notification System
 * Generic, secure toast notifications for security events and user feedback
 * Does not expose sensitive system details - shows user-friendly messages only
 */

class ToastNotification {
    constructor() {
        this.container = document.getElementById('toastContainer');
    }

    /**
     * Show a toast notification
     * @param {string} message - User-friendly message to display
     * @param {string} type - 'success', 'danger', 'warning', 'info'
     * @param {number} duration - How long to show (ms), 0 = manual dismiss
     */
    show(message, type = 'info', duration = 5000) {
        const toastId = `toast-${Date.now()}`;
        const bgClass = this._getBgClass(type);
        const icon = this._getIcon(type);

        const toastHTML = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header bg-${bgClass} text-white">
                    <i class="bi ${icon} me-2"></i>
                    <strong class="me-auto">${this._getTitle(type)}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${this._escapeHtml(message)}
                </div>
            </div>
        `;

        this.container.insertAdjacentHTML('beforeend', toastHTML);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { delay: duration });

        toast.show();

        // Remove element from DOM after it's hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });

        return toast;
    }

    /**
     * Show success notification
     */
    success(message, duration = 4000) {
        return this.show(message, 'success', duration);
    }

    /**
     * Show error/danger notification
     */
    danger(message, duration = 6000) {
        return this.show(message, 'danger', duration);
    }

    /**
     * Show warning notification
     */
    warning(message, duration = 5000) {
        return this.show(message, 'warning', duration);
    }

    /**
     * Show info notification
     */
    info(message, duration = 5000) {
        return this.show(message, 'info', duration);
    }

    /**
     * Show generic security alert (doesn't expose system details)
     */
    securityAlert(message = 'Security verification failed. Please try again.', duration = 6000) {
        return this.show(message, 'warning', duration);
    }

    /**
     * Show account locked alert (generic message)
     */
    accountLocked(minutesRemaining = null) {
        const message = minutesRemaining 
            ? `Account temporarily locked. Please try again in a few minutes.`
            : `Account temporarily locked for security. Please try again later.`;
        return this.show(message, 'danger', 0); // 0 = requires manual dismiss
    }

    /**
     * Show reCAPTCHA failure (generic message, no score leak)
     */
    captchaFailed(message = 'Security verification failed. Please try again.') {
        return this.show(message, 'warning', 6000);
    }

    /**
     * Show login failed (generic message)
     */
    loginFailed(message = 'Login failed. Please check your credentials and try again.') {
        return this.show(message, 'danger', 5000);
    }

    /**
     * Show validation error (generic message)
     */
    validationError(message = 'Please check your input and try again.') {
        return this.show(message, 'danger', 5000);
    }

    /**
     * Get Bootstrap background color class
     */
    _getBgClass(type) {
        const classes = {
            'success': 'success',
            'danger': 'danger',
            'warning': 'warning',
            'info': 'info'
        };
        return classes[type] || 'info';
    }

    /**
     * Get Bootstrap icon class
     */
    _getIcon(type) {
        const icons = {
            'success': 'bi-check-circle-fill',
            'danger': 'bi-exclamation-triangle-fill',
            'warning': 'bi-exclamation-circle-fill',
            'info': 'bi-info-circle-fill'
        };
        return icons[type] || 'bi-info-circle-fill';
    }

    /**
     * Get user-friendly title for notification type
     */
    _getTitle(type) {
        const titles = {
            'success': 'Success',
            'danger': 'Error',
            'warning': 'Warning',
            'info': 'Information'
        };
        return titles[type] || 'Notification';
    }

    /**
     * Escape HTML to prevent XSS
     */
    _escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize globally
window.toast = new ToastNotification();

/**
 * Helper function to display security event messages without leaking details
 * @param {string} eventType - Type of security event
 * @param {object} options - Additional options
 */
window.showSecurityAlert = function(eventType, options = {}) {
    const defaults = {
        message: 'A security event occurred. Please try again.',
        duration: 5000
    };

    const config = { ...defaults, ...options };

    switch(eventType) {
        case 'recaptcha_failed':
            window.toast.captchaFailed(config.message);
            break;
        case 'account_locked':
            window.toast.accountLocked();
            break;
        case 'login_failed':
            window.toast.loginFailed(config.message);
            break;
        case 'validation_error':
            window.toast.validationError(config.message);
            break;
        case 'session_expired':
            window.toast.warning('Your session has expired. Please log in again.', 5000);
            break;
        default:
            window.toast.info(config.message, config.duration);
    }
};

