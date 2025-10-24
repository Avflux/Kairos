window.kanbanFeedback = {
    // Notification system
    showNotification: function (message, type = 'info', duration = 3000) {
        const notification = this.createNotification(message, type);
        document.body.appendChild(notification);
        
        // Animate in
        requestAnimationFrame(() => {
            notification.classList.add('show');
        });
        
        // Auto remove
        setTimeout(() => {
            this.removeNotification(notification);
        }, duration);
        
        // Click to dismiss
        notification.addEventListener('click', () => {
            this.removeNotification(notification);
        });
        
        return notification;
    },

    createNotification: function (message, type) {
        const notification = document.createElement('div');
        notification.className = `kanban-notification kanban-notification-${type}`;
        
        const icon = this.getNotificationIcon(type);
        const closeBtn = document.createElement('button');
        closeBtn.className = 'notification-close';
        closeBtn.innerHTML = '×';
        closeBtn.setAttribute('aria-label', 'Fechar notificação');
        
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">${icon}</span>
                <span class="notification-message">${message}</span>
            </div>
        `;
        notification.appendChild(closeBtn);
        
        closeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeNotification(notification);
        });
        
        // Accessibility
        notification.setAttribute('role', 'alert');
        notification.setAttribute('aria-live', 'polite');
        notification.setAttribute('tabindex', '0');
        
        return notification;
    },

    getNotificationIcon: function (type) {
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };
        return icons[type] || icons.info;
    },

    removeNotification: function (notification) {
        notification.classList.add('hide');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    },

    // Loading indicators
    showLoading: function (message, operationId = null) {
        const loadingId = operationId || 'default-loading';
        
        // Remove existing loading for this operation
        this.hideLoading(operationId);
        
        const loading = document.createElement('div');
        loading.id = `loading-${loadingId}`;
        loading.className = 'kanban-loading-overlay';
        loading.setAttribute('role', 'status');
        loading.setAttribute('aria-live', 'polite');
        loading.setAttribute('aria-label', message);
        
        loading.innerHTML = `
            <div class="kanban-loading-content">
                <div class="kanban-loading-spinner"></div>
                <div class="kanban-loading-message">${message}</div>
            </div>
        `;
        
        document.body.appendChild(loading);
        
        // Animate in
        requestAnimationFrame(() => {
            loading.classList.add('show');
        });
    },

    hideLoading: function (operationId = null) {
        const loadingId = operationId || 'default-loading';
        const loading = document.getElementById(`loading-${loadingId}`);
        
        if (loading) {
            loading.classList.add('hide');
            setTimeout(() => {
                if (loading.parentNode) {
                    loading.parentNode.removeChild(loading);
                }
            }, 300);
        }
    },

    // Progress indicators
    showProgress: function (message, percentage, operationId = null) {
        const progressId = operationId || 'default-progress';
        let progress = document.getElementById(`progress-${progressId}`);
        
        if (!progress) {
            progress = document.createElement('div');
            progress.id = `progress-${progressId}`;
            progress.className = 'kanban-progress-overlay';
            progress.setAttribute('role', 'progressbar');
            progress.setAttribute('aria-valuemin', '0');
            progress.setAttribute('aria-valuemax', '100');
            
            progress.innerHTML = `
                <div class="kanban-progress-content">
                    <div class="kanban-progress-message">${message}</div>
                    <div class="kanban-progress-bar">
                        <div class="kanban-progress-fill"></div>
                    </div>
                    <div class="kanban-progress-percentage">0%</div>
                </div>
            `;
            
            document.body.appendChild(progress);
            
            // Animate in
            requestAnimationFrame(() => {
                progress.classList.add('show');
            });
        }
        
        // Update progress
        const fill = progress.querySelector('.kanban-progress-fill');
        const percentageText = progress.querySelector('.kanban-progress-percentage');
        const messageElement = progress.querySelector('.kanban-progress-message');
        
        if (fill) fill.style.width = `${percentage}%`;
        if (percentageText) percentageText.textContent = `${percentage}%`;
        if (messageElement) messageElement.textContent = message;
        
        progress.setAttribute('aria-valuenow', percentage);
        progress.setAttribute('aria-valuetext', `${percentage}% - ${message}`);
    },

    hideProgress: function (operationId = null) {
        const progressId = operationId || 'default-progress';
        const progress = document.getElementById(`progress-${progressId}`);
        
        if (progress) {
            progress.classList.add('hide');
            setTimeout(() => {
                if (progress.parentNode) {
                    progress.parentNode.removeChild(progress);
                }
            }, 300);
        }
    },

    // Haptic feedback (for mobile devices)
    hapticFeedback: function (type = 'light') {
        if ('vibrate' in navigator) {
            const patterns = {
                light: [10],
                medium: [20],
                heavy: [30],
                success: [10, 50, 10],
                warning: [20, 100, 20],
                error: [50, 100, 50, 100, 50]
            };
            
            const pattern = patterns[type] || patterns.light;
            navigator.vibrate(pattern);
        }
    },

    // Element animations
    animateElement: function (elementId, animation = 'bounce') {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        // Remove any existing animation classes
        element.classList.remove(...this.getAnimationClasses());
        
        // Add new animation class
        element.classList.add(`kanban-animate-${animation}`);
        
        // Remove animation class after completion
        const animationDuration = this.getAnimationDuration(animation);
        setTimeout(() => {
            element.classList.remove(`kanban-animate-${animation}`);
        }, animationDuration);
    },

    getAnimationClasses: function () {
        return [
            'kanban-animate-bounce',
            'kanban-animate-shake',
            'kanban-animate-pulse',
            'kanban-animate-fadein',
            'kanban-animate-fadeout',
            'kanban-animate-slideup',
            'kanban-animate-slidedown',
            'kanban-animate-slideleft',
            'kanban-animate-slideright',
            'kanban-animate-zoom',
            'kanban-animate-flip'
        ];
    },

    getAnimationDuration: function (animation) {
        const durations = {
            bounce: 600,
            shake: 500,
            pulse: 1000,
            fadein: 300,
            fadeout: 300,
            slideup: 300,
            slidedown: 300,
            slideleft: 300,
            slideright: 300,
            zoom: 300,
            flip: 600
        };
        return durations[animation] || 300;
    },

    // Element highlighting
    highlightElement: function (elementId, duration = 2000) {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        element.classList.add('kanban-highlight');
        
        setTimeout(() => {
            element.classList.remove('kanban-highlight');
        }, duration);
    },

    // Performance monitoring
    measurePerformance: function (operationName, operation) {
        const startTime = performance.now();
        
        const result = operation();
        
        if (result && typeof result.then === 'function') {
            // Handle async operations
            return result.then(value => {
                const endTime = performance.now();
                this.logPerformance(operationName, endTime - startTime);
                return value;
            }).catch(error => {
                const endTime = performance.now();
                this.logPerformance(operationName, endTime - startTime, true);
                throw error;
            });
        } else {
            // Handle sync operations
            const endTime = performance.now();
            this.logPerformance(operationName, endTime - startTime);
            return result;
        }
    },

    logPerformance: function (operationName, duration, isError = false) {
        const level = isError ? 'error' : (duration > 1000 ? 'warn' : 'info');
        console[level](`Performance [${operationName}]: ${duration.toFixed(2)}ms`);
        
        // Store performance data for analysis
        if (!window.kanbanPerformanceData) {
            window.kanbanPerformanceData = {};
        }
        
        if (!window.kanbanPerformanceData[operationName]) {
            window.kanbanPerformanceData[operationName] = [];
        }
        
        window.kanbanPerformanceData[operationName].push({
            duration,
            timestamp: Date.now(),
            isError
        });
    },

    // Debounce utility
    debounce: function (func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    },

    // Throttle utility
    throttle: function (func, limit) {
        let inThrottle;
        return function (...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    // Initialize feedback system
    init: function () {
        this.addFeedbackStyles();
        this.setupGlobalErrorHandling();
    },

    addFeedbackStyles: function () {
        if (document.getElementById('kanban-feedback-styles')) return;
        
        const style = document.createElement('style');
        style.id = 'kanban-feedback-styles';
        style.textContent = `
            /* Notification styles */
            .kanban-notification {
                position: fixed;
                top: 20px;
                right: 20px;
                min-width: 300px;
                max-width: 500px;
                padding: 1rem 1.5rem;
                border-radius: 8px;
                color: white;
                font-weight: 500;
                box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
                z-index: 10000;
                cursor: pointer;
                transform: translateX(100%);
                opacity: 0;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                display: flex;
                align-items: center;
                justify-content: space-between;
            }

            .kanban-notification.show {
                transform: translateX(0);
                opacity: 1;
            }

            .kanban-notification.hide {
                transform: translateX(100%);
                opacity: 0;
            }

            .kanban-notification-success {
                background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
            }

            .kanban-notification-error {
                background: linear-gradient(135deg, #dc3545 0%, #e74c3c 100%);
            }

            .kanban-notification-warning {
                background: linear-gradient(135deg, #ffc107 0%, #f39c12 100%);
                color: #333;
            }

            .kanban-notification-info {
                background: linear-gradient(135deg, #007bff 0%, #0056b3 100%);
            }

            .notification-content {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                flex: 1;
            }

            .notification-icon {
                font-size: 1.25rem;
                font-weight: bold;
            }

            .notification-message {
                flex: 1;
                line-height: 1.4;
            }

            .notification-close {
                background: none;
                border: none;
                color: inherit;
                font-size: 1.5rem;
                cursor: pointer;
                padding: 0;
                margin-left: 1rem;
                opacity: 0.8;
                transition: opacity 0.2s;
            }

            .notification-close:hover {
                opacity: 1;
            }

            /* Loading overlay styles */
            .kanban-loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.7);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 10000;
                opacity: 0;
                transition: opacity 0.3s ease;
            }

            .kanban-loading-overlay.show {
                opacity: 1;
            }

            .kanban-loading-overlay.hide {
                opacity: 0;
            }

            .kanban-loading-content {
                background: white;
                padding: 2rem;
                border-radius: 12px;
                text-align: center;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                min-width: 200px;
            }

            .kanban-loading-spinner {
                width: 40px;
                height: 40px;
                border: 4px solid #e9ecef;
                border-top: 4px solid #007bff;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 1rem;
            }

            .kanban-loading-message {
                color: #333;
                font-weight: 500;
                font-size: 1rem;
            }

            /* Progress overlay styles */
            .kanban-progress-overlay {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.7);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 10000;
                opacity: 0;
                transition: opacity 0.3s ease;
            }

            .kanban-progress-overlay.show {
                opacity: 1;
            }

            .kanban-progress-overlay.hide {
                opacity: 0;
            }

            .kanban-progress-content {
                background: white;
                padding: 2rem;
                border-radius: 12px;
                text-align: center;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                min-width: 300px;
            }

            .kanban-progress-message {
                color: #333;
                font-weight: 500;
                font-size: 1rem;
                margin-bottom: 1rem;
            }

            .kanban-progress-bar {
                width: 100%;
                height: 8px;
                background: #e9ecef;
                border-radius: 4px;
                overflow: hidden;
                margin-bottom: 0.5rem;
            }

            .kanban-progress-fill {
                height: 100%;
                background: linear-gradient(90deg, #007bff, #0056b3);
                border-radius: 4px;
                transition: width 0.3s ease;
                width: 0%;
            }

            .kanban-progress-percentage {
                color: #666;
                font-size: 0.875rem;
                font-weight: 600;
            }

            /* Animation classes */
            .kanban-animate-bounce {
                animation: kanban-bounce 0.6s ease-out;
            }

            .kanban-animate-shake {
                animation: kanban-shake 0.5s ease-out;
            }

            .kanban-animate-pulse {
                animation: kanban-pulse 1s ease-out;
            }

            .kanban-animate-fadein {
                animation: kanban-fadein 0.3s ease-out;
            }

            .kanban-animate-fadeout {
                animation: kanban-fadeout 0.3s ease-out;
            }

            .kanban-animate-slideup {
                animation: kanban-slideup 0.3s ease-out;
            }

            .kanban-animate-slidedown {
                animation: kanban-slidedown 0.3s ease-out;
            }

            .kanban-animate-slideleft {
                animation: kanban-slideleft 0.3s ease-out;
            }

            .kanban-animate-slideright {
                animation: kanban-slideright 0.3s ease-out;
            }

            .kanban-animate-zoom {
                animation: kanban-zoom 0.3s ease-out;
            }

            .kanban-animate-flip {
                animation: kanban-flip 0.6s ease-out;
            }

            /* Highlight effect */
            .kanban-highlight {
                position: relative;
                z-index: 1000;
            }

            .kanban-highlight::before {
                content: '';
                position: absolute;
                top: -4px;
                left: -4px;
                right: -4px;
                bottom: -4px;
                background: linear-gradient(45deg, #007bff, #0056b3, #007bff);
                border-radius: 8px;
                z-index: -1;
                animation: kanban-highlight-pulse 2s ease-out;
            }

            /* Keyframe animations */
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }

            @keyframes kanban-bounce {
                0%, 20%, 53%, 80%, 100% { transform: translate3d(0, 0, 0); }
                40%, 43% { transform: translate3d(0, -30px, 0); }
                70% { transform: translate3d(0, -15px, 0); }
                90% { transform: translate3d(0, -4px, 0); }
            }

            @keyframes kanban-shake {
                0%, 100% { transform: translateX(0); }
                10%, 30%, 50%, 70%, 90% { transform: translateX(-10px); }
                20%, 40%, 60%, 80% { transform: translateX(10px); }
            }

            @keyframes kanban-pulse {
                0% { transform: scale(1); }
                50% { transform: scale(1.1); }
                100% { transform: scale(1); }
            }

            @keyframes kanban-fadein {
                0% { opacity: 0; }
                100% { opacity: 1; }
            }

            @keyframes kanban-fadeout {
                0% { opacity: 1; }
                100% { opacity: 0; }
            }

            @keyframes kanban-slideup {
                0% { transform: translateY(20px); opacity: 0; }
                100% { transform: translateY(0); opacity: 1; }
            }

            @keyframes kanban-slidedown {
                0% { transform: translateY(-20px); opacity: 0; }
                100% { transform: translateY(0); opacity: 1; }
            }

            @keyframes kanban-slideleft {
                0% { transform: translateX(20px); opacity: 0; }
                100% { transform: translateX(0); opacity: 1; }
            }

            @keyframes kanban-slideright {
                0% { transform: translateX(-20px); opacity: 0; }
                100% { transform: translateX(0); opacity: 1; }
            }

            @keyframes kanban-zoom {
                0% { transform: scale(0.8); opacity: 0; }
                100% { transform: scale(1); opacity: 1; }
            }

            @keyframes kanban-flip {
                0% { transform: rotateY(-90deg); opacity: 0; }
                100% { transform: rotateY(0); opacity: 1; }
            }

            @keyframes kanban-highlight-pulse {
                0%, 100% { opacity: 0; }
                50% { opacity: 0.8; }
            }

            /* Responsive design */
            @media (max-width: 768px) {
                .kanban-notification {
                    right: 10px;
                    left: 10px;
                    min-width: auto;
                    max-width: none;
                }

                .kanban-loading-content,
                .kanban-progress-content {
                    margin: 1rem;
                    min-width: auto;
                    width: calc(100vw - 2rem);
                    max-width: 400px;
                }
            }

            /* Reduced motion support */
            @media (prefers-reduced-motion: reduce) {
                .kanban-notification,
                .kanban-loading-overlay,
                .kanban-progress-overlay {
                    transition: none;
                }

                .kanban-animate-bounce,
                .kanban-animate-shake,
                .kanban-animate-pulse,
                .kanban-animate-fadein,
                .kanban-animate-fadeout,
                .kanban-animate-slideup,
                .kanban-animate-slidedown,
                .kanban-animate-slideleft,
                .kanban-animate-slideright,
                .kanban-animate-zoom,
                .kanban-animate-flip {
                    animation: none;
                }

                .kanban-loading-spinner {
                    animation: none;
                    border-top-color: transparent;
                }
            }
        `;
        document.head.appendChild(style);
    },

    setupGlobalErrorHandling: function () {
        // Handle unhandled promise rejections
        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled promise rejection:', event.reason);
            this.showNotification('Ocorreu um erro inesperado. Tente novamente.', 'error');
        });

        // Handle JavaScript errors
        window.addEventListener('error', (event) => {
            console.error('JavaScript error:', event.error);
            this.showNotification('Ocorreu um erro inesperado. Tente novamente.', 'error');
        });
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        kanbanFeedback.init();
    });
} else {
    kanbanFeedback.init();
}