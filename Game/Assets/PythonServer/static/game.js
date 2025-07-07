// Конфигурация API
const API_BASE_URL = window.location.origin;
const ADMIN_TOKEN = 'ZXCVBNM,1234567890';

// Класс для работы с куками
class CookieManager {
    static setCookie(name, value, days = 30) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/`;
    }

    static getCookie(name) {
        const nameEQ = name + "=";
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) === ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }

    static deleteCookie(name) {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;`;
    }
}

// Класс для работы с API
class GameAPI {
    constructor() {
        this.baseUrl = API_BASE_URL;
        this.adminToken = ADMIN_TOKEN;
    }

    async makeRequest(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.adminToken}`,
                ...options.headers
            },
            ...options
        };

        try {
            const response = await fetch(url, config);
            const data = await response.json();
            
            this.logApiCall(endpoint, response.status, data, 'success');
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${data.message || 'Unknown error'}`);
            }
            
            return data;
        } catch (error) {
            this.logApiCall(endpoint, 'ERROR', { error: error.message }, 'error');
            throw error;
        }
    }

    // Лобби API
    async getLobbyStatus() {
        return this.makeRequest('/api-game-lobby/');
    }

    async getLobbyUsers(page = 1, perPage = 1000) {
        return this.makeRequest(`/api-game-lobby/user?page=${page}&per_page=${perPage}`);
    }

    async createLobbyUser(userData) {
        return this.makeRequest('/api-game-lobby/user', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async updateLobbyUser(userData) {
        return this.makeRequest('/api-game-lobby/user', {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    async deleteLobbyUser(userId) {
        return this.makeRequest('/api-game-lobby/user', {
            method: 'DELETE',
            body: JSON.stringify({ user_id: userId })
        });
    }

    // Очередь API
    async getQueueUsers() {
        return this.makeRequest('/api-game-queue/');
    }

    async addUserToQueue(userData) {
        return this.makeRequest('/api-game-queue/', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async removeUserFromQueue(userId) {
        return this.makeRequest('/api-game-queue/', {
            method: 'DELETE',
            body: JSON.stringify({ user_id: userId })
        });
    }

    // Игры API
    async getGames() {
        return this.makeRequest('/api-game-game/');
    }

    async createGame(gameData) {
        return this.makeRequest('/api-game-game/', {
            method: 'POST',
            body: JSON.stringify(gameData)
        });
    }

    async updateGame(gameData) {
        return this.makeRequest('/api-game-game/', {
            method: 'PUT',
            body: JSON.stringify(gameData)
        });
    }

    async deleteGame(gameId) {
        return this.makeRequest('/api-game-game/', {
            method: 'DELETE',
            body: JSON.stringify({ game_id: gameId })
        });
    }

    // Пользователи API
    async getUser(userId) {
        return this.makeRequest(`/api-game-user/?user_id=${userId}`);
    }

    async createUser(userData) {
        return this.makeRequest('/api-game-user/', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async updateUser(userData) {
        return this.makeRequest('/api-game-user/', {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    async deleteUser(userId) {
        return this.makeRequest('/api-game-user/', {
            method: 'DELETE',
            body: JSON.stringify({ user_id: userId })
        });
    }

    // Логирование API вызовов
    logApiCall(endpoint, status, data, type = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('div');
        logEntry.className = `log-entry log-${type}`;
        
        let logText = `[${timestamp}] ${endpoint} - Status: ${status}`;
        if (type === 'error') {
            logText += `\nError: ${data.error}`;
        } else if (data.message) {
            logText += `\nMessage: ${data.message}`;
        }
        
        if (data.users && Array.isArray(data.users)) {
            logText += `\nUsers count: ${data.users.length}`;
        }
        if (data.games && Array.isArray(data.games)) {
            logText += `\nGames count: ${data.games.length}`;
        }
        
        logEntry.textContent = logText;
        
        const logsContainer = document.getElementById('api-logs');
        if (logsContainer) {
            logsContainer.appendChild(logEntry);
            logsContainer.scrollTop = logsContainer.scrollHeight;
        }
    }
}

// Класс для управления авторизацией
class AuthManager {
    constructor(api) {
        this.api = api;
        this.currentUser = null;
        this.selectedAvatar = '1';
        this.initializeEventListeners();
        this.checkAuthStatus();
    }

    initializeEventListeners() {
        // Поле ввода имени
        const usernameInput = document.getElementById('username-input');
        if (usernameInput) {
            usernameInput.addEventListener('input', () => this.validateForm());
            usernameInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.startGame();
                }
            });
        }

        // Выбор аватара
        const avatarOptions = document.querySelectorAll('.avatar-option');
        avatarOptions.forEach(option => {
            option.addEventListener('click', () => {
                avatarOptions.forEach(opt => opt.classList.remove('selected'));
                option.classList.add('selected');
                this.selectedAvatar = option.dataset.avatar;
                this.validateForm();
            });
        });

        // Кнопка старта игры
        const startBtn = document.getElementById('start-game-btn');
        if (startBtn) {
            startBtn.addEventListener('click', () => this.startGame());
        }

        // Кнопка выхода
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', () => this.logout());
        }
    }

    validateForm() {
        const usernameInput = document.getElementById('username-input');
        const startBtn = document.getElementById('start-game-btn');
        
        if (!usernameInput || !startBtn) return;

        const username = usernameInput.value.trim();
        const isValid = username.length >= 3 && username.length <= 20;
        
        startBtn.disabled = !isValid;
        
        if (isValid) {
            startBtn.classList.add('ready');
        } else {
            startBtn.classList.remove('ready');
        }
    }

    async checkAuthStatus() {
        const userCookie = CookieManager.getCookie('game_user');
        
        if (userCookie) {
            try {
                const userData = JSON.parse(userCookie);
                this.currentUser = userData;
                this.showGameScreen();
                this.updatePlayerInfo();
                return true;
            } catch (error) {
                console.error('Ошибка парсинга куки пользователя:', error);
                CookieManager.deleteCookie('game_user');
            }
        }
        
        this.showAuthScreen();
        await this.checkServerStatus();
        return false;
    }

    async checkServerStatus() {
        try {
            const status = await this.api.getLobbyStatus();
            this.updateServerStatus(true, `Онлайн (${status.users_count || 0} пользователей)`);
        } catch (error) {
            this.updateServerStatus(false, 'Офлайн');
        }
    }

    updateServerStatus(isOnline, message) {
        const statusDot = document.querySelector('.status-dot');
        const statusText = document.getElementById('server-status-text');
        
        if (statusDot) {
            statusDot.classList.toggle('online', isOnline);
        }
        
        if (statusText) {
            statusText.textContent = message;
        }
    }

    async startGame() {
        const usernameInput = document.getElementById('username-input');
        const startBtn = document.getElementById('start-game-btn');
        const loadingIndicator = document.getElementById('loading-indicator');
        
        if (!usernameInput || !startBtn || !loadingIndicator) return;

        const username = usernameInput.value.trim();
        
        if (username.length < 3 || username.length > 20) {
            return;
        }

        // Показываем индикатор загрузки
        startBtn.style.display = 'none';
        loadingIndicator.style.display = 'flex';

        try {
            // Создаем пользователя
            const userId = 'user_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            const userData = {
                user_id: userId,
                username: username,
                avatar: this.selectedAvatar,
                email: `${username.toLowerCase()}@game.local`,
                status: 'active',
                created_at: new Date().toISOString()
            };

            // Создаем пользователя в системе
            await this.api.createUser(userData);
            await this.api.createLobbyUser(userData);

            // Сохраняем в куки
            this.currentUser = userData;
            CookieManager.setCookie('game_user', JSON.stringify(userData));

            // Переходим к игре
            this.showGameScreen();
            this.updatePlayerInfo();

        } catch (error) {
            console.error('Ошибка создания пользователя:', error);
            alert('Ошибка подключения к серверу. Попробуйте еще раз.');
        } finally {
            // Скрываем индикатор загрузки
            loadingIndicator.style.display = 'none';
            startBtn.style.display = 'block';
        }
    }

    showAuthScreen() {
        const authScreen = document.getElementById('auth-screen');
        const gameScreen = document.getElementById('game-screen');
        
        if (authScreen) authScreen.style.display = 'flex';
        if (gameScreen) gameScreen.style.display = 'none';
    }

    showGameScreen() {
        const authScreen = document.getElementById('auth-screen');
        const gameScreen = document.getElementById('game-screen');
        
        if (authScreen) authScreen.style.display = 'none';
        if (gameScreen) gameScreen.style.display = 'block';
        
        // Загружаем Unity игру
        this.loadUnityGame();
    }

    loadUnityGame() {
        const iframe = document.getElementById('unity-game-frame');
        const container = document.querySelector('.game-frame-container');
        
        if (!iframe || !container) return;
        
        // Показываем индикатор загрузки
        container.classList.remove('loaded');
        
        // Обработчик загрузки iframe
        iframe.onload = () => {
            console.log('Unity game loaded successfully');
            container.classList.add('loaded');
        };
        
        // Обработчик ошибок
        iframe.onerror = () => {
            console.error('Failed to load Unity game');
            container.classList.add('loaded');
        };
        
        // Устанавливаем таймаут для загрузки
        setTimeout(() => {
            container.classList.add('loaded');
        }, 10000); // 10 секунд таймаут
    }

    updatePlayerInfo() {
        if (!this.currentUser) return;

        const playerAvatar = document.getElementById('player-avatar');
        const playerName = document.getElementById('player-name');
        const playerId = document.getElementById('player-id');

        if (playerAvatar) {
            const avatars = {
                '1': '👤', '2': '🤖', '3': '👾', 
                '4': '🦄', '5': '🐉', '6': '⚡'
            };
            playerAvatar.textContent = avatars[this.currentUser.avatar] || '👤';
        }

        if (playerName) {
            playerName.textContent = this.currentUser.username;
        }

        if (playerId) {
            playerId.textContent = `ID: ${this.currentUser.user_id}`;
        }
    }

    logout() {
        if (confirm('Вы уверены, что хотите выйти?')) {
            CookieManager.deleteCookie('game_user');
            this.currentUser = null;
            this.showAuthScreen();
            
            // Очищаем форму
            const usernameInput = document.getElementById('username-input');
            if (usernameInput) {
                usernameInput.value = '';
            }
            
            // Сбрасываем выбор аватара
            const avatarOptions = document.querySelectorAll('.avatar-option');
            avatarOptions.forEach(opt => opt.classList.remove('selected'));
            if (avatarOptions[0]) {
                avatarOptions[0].classList.add('selected');
                this.selectedAvatar = '1';
            }
            
            this.validateForm();
        }
    }
}

// Класс для управления Dashboard
class DashboardManager {
    constructor(api) {
        this.api = api;
        this.initializeEventListeners();
    }

    initializeEventListeners() {
        // Кнопка открытия dashboard
        const dashboardBtn = document.getElementById('dashboard-btn');
        if (dashboardBtn) {
            dashboardBtn.addEventListener('click', () => this.showDashboard());
        }

        // Кнопка закрытия dashboard
        const closeDashboardBtn = document.getElementById('close-dashboard');
        if (closeDashboardBtn) {
            closeDashboardBtn.addEventListener('click', () => this.hideDashboard());
        }

        // Кнопки управления
        document.getElementById('refresh-lobby')?.addEventListener('click', () => this.loadLobbyUsers());
        document.getElementById('create-user')?.addEventListener('click', () => this.createRandomUser());
        document.getElementById('refresh-queue')?.addEventListener('click', () => this.loadQueueUsers());
        document.getElementById('join-queue')?.addEventListener('click', () => this.joinQueue());
        document.getElementById('refresh-games')?.addEventListener('click', () => this.loadGames());
        document.getElementById('create-game')?.addEventListener('click', () => this.createRandomGame());
        document.getElementById('test-all-apis')?.addEventListener('click', () => this.testAllAPIs());
        document.getElementById('clear-logs')?.addEventListener('click', () => this.clearLogs());
    }

    showDashboard() {
        const dashboardPanel = document.getElementById('dashboard-panel');
        if (dashboardPanel) {
            dashboardPanel.style.display = 'block';
            this.loadAllData();
        }
    }

    hideDashboard() {
        const dashboardPanel = document.getElementById('dashboard-panel');
        if (dashboardPanel) {
            dashboardPanel.style.display = 'none';
        }
    }

    async loadAllData() {
        await Promise.all([
            this.loadLobbyUsers(),
            this.loadQueueUsers(),
            this.loadGames()
        ]);
    }

    async loadLobbyUsers() {
        const container = document.getElementById('lobby-users');
        if (!container) return;

        container.innerHTML = '<p>Загрузка пользователей...</p>';

        try {
            const data = await this.api.getLobbyUsers();
            this.displayUsers(container, data.users, 'lobby');
        } catch (error) {
            container.innerHTML = '<p>Ошибка загрузки пользователей</p>';
        }
    }

    async loadQueueUsers() {
        const container = document.getElementById('queue-users');
        if (!container) return;

        container.innerHTML = '<p>Загрузка очереди...</p>';

        try {
            const data = await this.api.getQueueUsers();
            this.displayUsers(container, data.users, 'queue');
        } catch (error) {
            container.innerHTML = '<p>Ошибка загрузки очереди</p>';
        }
    }

    async loadGames() {
        const container = document.getElementById('games-list');
        if (!container) return;

        container.innerHTML = '<p>Загрузка игр...</p>';

        try {
            const data = await this.api.getGames();
            this.displayGames(container, data.games);
        } catch (error) {
            container.innerHTML = '<p>Ошибка загрузки игр</p>';
        }
    }

    displayUsers(container, users, type) {
        if (!users || users.length === 0) {
            container.innerHTML = '<p>Нет пользователей</p>';
            return;
        }

        container.innerHTML = users.map(user => `
            <div class="user-item">
                <h4>${user.username || 'Без имени'}</h4>
                <p>ID: ${user.user_id}</p>
                <p>Статус: ${user.status || 'неизвестно'}</p>
                ${type === 'queue' ? `<p>Приоритет: ${user.priority || 0}</p>` : ''}
                ${user.created_at ? `<p>Создан: ${new Date(user.created_at).toLocaleString()}</p>` : ''}
            </div>
        `).join('');
    }

    displayGames(container, games) {
        if (!games || games.length === 0) {
            container.innerHTML = '<p>Нет игр</p>';
            return;
        }

        container.innerHTML = games.map(game => `
            <div class="game-item">
                <h4>${game.name || 'Без названия'}</h4>
                <p>ID: ${game.game_id}</p>
                <p>Статус: ${game.status || 'неизвестно'}</p>
                <p>Игроки: ${game.current_players || 0}/${game.max_players || 4}</p>
                ${game.created_at ? `<p>Создана: ${new Date(game.created_at).toLocaleString()}</p>` : ''}
            </div>
        `).join('');
    }

    async createRandomUser() {
        const userId = 'user_' + Date.now();
        const userData = {
            user_id: userId,
            username: `Player_${Math.floor(Math.random() * 1000)}`,
            email: `player${Math.floor(Math.random() * 1000)}@example.com`,
            status: 'active'
        };

        try {
            await this.api.createUser(userData);
            await this.api.createLobbyUser(userData);
            this.loadLobbyUsers();
        } catch (error) {
            console.error('Ошибка создания пользователя:', error);
        }
    }

    async joinQueue() {
        const userId = 'queue_user_' + Date.now();
        const userData = {
            user_id: userId,
            username: `QueuePlayer_${Math.floor(Math.random() * 1000)}`,
            priority: Math.floor(Math.random() * 5) + 1,
            status: 'waiting'
        };

        try {
            await this.api.addUserToQueue(userData);
            this.loadQueueUsers();
        } catch (error) {
            console.error('Ошибка присоединения к очереди:', error);
        }
    }

    async createRandomGame() {
        const gameId = 'game_' + Date.now();
        const gameData = {
            game_id: gameId,
            name: `Game_${Math.floor(Math.random() * 1000)}`,
            max_players: Math.floor(Math.random() * 4) + 2,
            status: 'waiting'
        };

        try {
            await this.api.createGame(gameData);
            this.loadGames();
        } catch (error) {
            console.error('Ошибка создания игры:', error);
        }
    }

    async testAllAPIs() {
        this.api.logApiCall('TEST_ALL_APIS', 'START', {}, 'info');
        
        try {
            await this.api.getLobbyStatus();
            await this.api.getLobbyUsers();
            await this.api.getQueueUsers();
            await this.api.getGames();
            
            this.api.logApiCall('TEST_ALL_APIS', 'COMPLETE', { message: 'Все API протестированы успешно' }, 'success');
        } catch (error) {
            this.api.logApiCall('TEST_ALL_APIS', 'ERROR', { error: error.message }, 'error');
        }
    }

    clearLogs() {
        const logsContainer = document.getElementById('api-logs');
        if (logsContainer) {
            logsContainer.innerHTML = '<p>Логи API запросов появятся здесь...</p>';
        }
    }
}

// Инициализация приложения
document.addEventListener('DOMContentLoaded', () => {
    const api = new GameAPI();
    const authManager = new AuthManager(api);
    const dashboardManager = new DashboardManager(api);
    
    // Периодическая проверка статуса сервера
    setInterval(() => authManager.checkServerStatus(), 30000);
}); 