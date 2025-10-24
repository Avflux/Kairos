window.kanbanAccessibility = {
    // Screen reader announcements
    announce: function (message) {
        const announcer = this.getOrCreateAnnouncer();
        announcer.textContent = message;
        
        // Clear after announcement to allow repeated messages
        setTimeout(() => {
            announcer.textContent = '';
        }, 1000);
    },

    getOrCreateAnnouncer: function () {
        let announcer = document.getElementById('kanban-announcer');
        if (!announcer) {
            announcer = document.createElement('div');
            announcer.id = 'kanban-announcer';
            announcer.setAttribute('aria-live', 'polite');
            announcer.setAttribute('aria-atomic', 'true');
            announcer.className = 'sr-only';
            document.body.appendChild(announcer);
        }
        return announcer;
    },

    // Focus management
    trapFocus: function (shiftKey) {
        const dialog = document.querySelector('.add-board-dialog');
        if (!dialog) return;

        const focusableElements = dialog.querySelectorAll(
            'input, button, textarea, select, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;

        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];
        
        if (shiftKey && document.activeElement === firstElement) {
            lastElement.focus();
            return false;
        } else if (!shiftKey && document.activeElement === lastElement) {
            firstElement.focus();
            return false;
        }
        
        return true;
    },

    // Board navigation
    navigateToBoard: function (currentIndex, totalBoards, moveRight) {
        const boards = document.querySelectorAll('.board-column');
        if (!boards || boards.length === 0) return;

        let targetIndex;
        if (moveRight) {
            targetIndex = currentIndex + 1 >= totalBoards ? 0 : currentIndex + 1;
        } else {
            targetIndex = currentIndex - 1 < 0 ? totalBoards - 1 : currentIndex - 1;
        }

        if (boards[targetIndex]) {
            boards[targetIndex].focus();
            this.announce(`Navegando para quadro ${targetIndex + 1} de ${totalBoards}`);
        }
    },

    navigateToBoardFromCard: function (currentBoardId, moveRight) {
        const currentBoard = document.querySelector(`[data-board-id="${currentBoardId}"]`);
        if (!currentBoard) return;

        const allBoards = Array.from(document.querySelectorAll('.board-column'));
        const currentIndex = allBoards.indexOf(currentBoard);
        
        if (currentIndex === -1) return;

        let targetIndex;
        if (moveRight) {
            targetIndex = currentIndex + 1 >= allBoards.length ? 0 : currentIndex + 1;
        } else {
            targetIndex = currentIndex - 1 < 0 ? allBoards.length - 1 : currentIndex - 1;
        }

        if (allBoards[targetIndex]) {
            allBoards[targetIndex].focus();
            this.announce(`Navegando para quadro ${targetIndex + 1} de ${allBoards.length}`);
        }
    },

    // Card navigation
    navigateToCard: function (currentCardId, currentIndex, totalCards, moveDown) {
        const currentCard = document.querySelector(`[data-card-id="${currentCardId}"]`);
        if (!currentCard) return;

        const boardContainer = currentCard.closest('.board-column');
        if (!boardContainer) return;

        const cards = boardContainer.querySelectorAll('.task-card');
        
        let targetIndex;
        if (moveDown) {
            targetIndex = currentIndex + 1 >= totalCards ? 0 : currentIndex + 1;
        } else {
            targetIndex = currentIndex - 1 < 0 ? totalCards - 1 : currentIndex - 1;
        }

        if (cards[targetIndex]) {
            cards[targetIndex].focus();
            this.announce(`Navegando para cartão ${targetIndex + 1} de ${totalCards}`);
        }
    },

    navigateToFirstCard: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (!board) return;

        const firstCard = board.querySelector('.task-card');
        if (firstCard) {
            firstCard.focus();
            this.announce('Navegando para primeiro cartão');
        }
    },

    navigateToLastCard: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (!board) return;

        const cards = board.querySelectorAll('.task-card');
        const lastCard = cards[cards.length - 1];
        if (lastCard) {
            lastCard.focus();
            this.announce('Navegando para último cartão');
        }
    },

    focusFirstCard: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (!board) return;

        const firstCard = board.querySelector('.task-card');
        if (firstCard) {
            firstCard.focus();
            this.announce('Focando primeiro cartão do quadro');
        } else {
            this.announce('Quadro não possui cartões');
        }
    },

    focusBoard: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (board) {
            board.focus();
            this.announce('Retornando ao quadro');
        }
    },

    // Board movement with keyboard
    startBoardMove: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (!board) return;

        board.classList.add('keyboard-moving');
        board.setAttribute('data-moving', 'true');
        
        // Add visual indicator
        const indicator = document.createElement('div');
        indicator.className = 'keyboard-move-indicator';
        indicator.textContent = 'Modo de movimentação ativo - Use ← → para mover, Enter para confirmar, Escape para cancelar';
        board.appendChild(indicator);
    },

    endBoardMove: function (boardId) {
        const board = document.querySelector(`[data-board-id="${boardId}"]`);
        if (!board) return;

        board.classList.remove('keyboard-moving');
        board.removeAttribute('data-moving');
        
        const indicator = board.querySelector('.keyboard-move-indicator');
        if (indicator) {
            indicator.remove();
        }
    },

    // Keyboard help
    showKeyboardHelp: function () {
        const existingHelp = document.getElementById('keyboard-help-modal');
        if (existingHelp) {
            existingHelp.remove();
        }

        const helpModal = document.createElement('div');
        helpModal.id = 'keyboard-help-modal';
        helpModal.className = 'keyboard-help-modal';
        helpModal.setAttribute('role', 'dialog');
        helpModal.setAttribute('aria-modal', 'true');
        helpModal.setAttribute('aria-labelledby', 'help-title');

        helpModal.innerHTML = `
            <div class="keyboard-help-content">
                <h2 id="help-title">Atalhos de Teclado - Sistema Kanban</h2>
                <div class="help-sections">
                    <div class="help-section">
                        <h3>Navegação Geral</h3>
                        <ul>
                            <li><kbd>Tab</kbd> / <kbd>Shift+Tab</kbd> - Navegar entre elementos</li>
                            <li><kbd>F1</kbd> - Mostrar esta ajuda</li>
                            <li><kbd>Escape</kbd> - Cancelar ação atual</li>
                        </ul>
                    </div>
                    <div class="help-section">
                        <h3>Quadros</h3>
                        <ul>
                            <li><kbd>Ctrl+Shift+N</kbd> - Novo quadro</li>
                            <li><kbd>Enter</kbd> - Editar título do quadro</li>
                            <li><kbd>Delete</kbd> - Remover quadro</li>
                            <li><kbd>← →</kbd> - Navegar entre quadros</li>
                            <li><kbd>↓</kbd> - Ir para primeiro cartão</li>
                        </ul>
                    </div>
                    <div class="help-section">
                        <h3>Cartões</h3>
                        <ul>
                            <li><kbd>Ctrl+Shift+C</kbd> - Novo cartão</li>
                            <li><kbd>Enter</kbd> - Editar cartão</li>
                            <li><kbd>Delete</kbd> - Remover cartão</li>
                            <li><kbd>↑ ↓</kbd> - Navegar entre cartões</li>
                            <li><kbd>← →</kbd> - Navegar entre quadros</li>
                            <li><kbd>Home</kbd> - Primeiro cartão</li>
                            <li><kbd>End</kbd> - Último cartão</li>
                        </ul>
                    </div>
                </div>
                <button class="help-close-btn" onclick="kanbanAccessibility.closeKeyboardHelp()">
                    Fechar (Escape)
                </button>
            </div>
        `;

        document.body.appendChild(helpModal);
        
        // Focus the close button
        const closeBtn = helpModal.querySelector('.help-close-btn');
        if (closeBtn) {
            closeBtn.focus();
        }

        // Handle escape key
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                this.closeKeyboardHelp();
                document.removeEventListener('keydown', handleEscape);
            }
        };
        document.addEventListener('keydown', handleEscape);

        this.announce('Ajuda de teclado aberta');
    },

    closeKeyboardHelp: function () {
        const helpModal = document.getElementById('keyboard-help-modal');
        if (helpModal) {
            helpModal.remove();
            this.announce('Ajuda de teclado fechada');
        }
    },

    // High contrast mode detection
    detectHighContrast: function () {
        return window.matchMedia('(prefers-contrast: high)').matches;
    },

    // Reduced motion detection
    detectReducedMotion: function () {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    },

    // Initialize accessibility features
    init: function () {
        // Add global styles for accessibility
        this.addAccessibilityStyles();
        
        // Set up global keyboard listeners
        this.setupGlobalKeyboardListeners();
        
        // Announce system ready
        setTimeout(() => {
            this.announce('Sistema Kanban carregado. Pressione F1 para ajuda de teclado.');
        }, 1000);
    },

    addAccessibilityStyles: function () {
        const style = document.createElement('style');
        style.textContent = `
            .sr-only {
                position: absolute !important;
                width: 1px !important;
                height: 1px !important;
                padding: 0 !important;
                margin: -1px !important;
                overflow: hidden !important;
                clip: rect(0, 0, 0, 0) !important;
                white-space: nowrap !important;
                border: 0 !important;
            }

            .keyboard-help-modal {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.8);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 10000;
            }

            .keyboard-help-content {
                background: white;
                padding: 2rem;
                border-radius: 8px;
                max-width: 600px;
                max-height: 80vh;
                overflow-y: auto;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
            }

            .keyboard-help-content h2 {
                margin-top: 0;
                color: #333;
            }

            .help-sections {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                gap: 1.5rem;
                margin: 1.5rem 0;
            }

            .help-section h3 {
                color: #555;
                margin-bottom: 0.5rem;
            }

            .help-section ul {
                list-style: none;
                padding: 0;
                margin: 0;
            }

            .help-section li {
                padding: 0.25rem 0;
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }

            .help-section kbd {
                background: #f0f0f0;
                border: 1px solid #ccc;
                border-radius: 3px;
                padding: 0.1rem 0.3rem;
                font-size: 0.85em;
                font-family: monospace;
                min-width: 1.5rem;
                text-align: center;
            }

            .help-close-btn {
                background: #007bff;
                color: white;
                border: none;
                padding: 0.75rem 1.5rem;
                border-radius: 4px;
                cursor: pointer;
                font-size: 1rem;
                margin-top: 1rem;
                width: 100%;
            }

            .help-close-btn:hover {
                background: #0056b3;
            }

            .help-close-btn:focus {
                outline: 2px solid #0056b3;
                outline-offset: 2px;
            }

            .keyboard-moving {
                outline: 3px solid #007bff !important;
                outline-offset: 2px;
                position: relative;
            }

            .keyboard-move-indicator {
                position: absolute;
                top: -30px;
                left: 0;
                right: 0;
                background: #007bff;
                color: white;
                padding: 0.5rem;
                border-radius: 4px;
                font-size: 0.875rem;
                text-align: center;
                z-index: 1000;
            }

            /* High contrast mode support */
            @media (prefers-contrast: high) {
                .keyboard-help-content {
                    border: 2px solid;
                }
                
                .help-section kbd {
                    border: 2px solid;
                }
            }

            /* Focus indicators */
            .task-card:focus,
            .board-column:focus,
            .btn-add-board:focus,
            .btn-add-card:focus,
            .btn-delete-board:focus {
                outline: 2px solid #007bff;
                outline-offset: 2px;
            }

            /* Reduced motion support */
            @media (prefers-reduced-motion: reduce) {
                .keyboard-help-modal {
                    animation: none;
                }
                
                .keyboard-moving {
                    transition: none;
                }
            }
        `;
        document.head.appendChild(style);
    },

    setupGlobalKeyboardListeners: function () {
        document.addEventListener('keydown', (e) => {
            // Global F1 help
            if (e.key === 'F1') {
                e.preventDefault();
                this.showKeyboardHelp();
            }
        });
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        kanbanAccessibility.init();
    });
} else {
    kanbanAccessibility.init();
}