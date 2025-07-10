// SECS Game Landing Page JavaScript
class GameAuth {
    constructor() {
        // Используем относительные пути - nginx проксирует все запросы
        this.baseUrl = '/api-game-player';
        this.gameUrl = 'https://renderfin.com/Game/index.html';
        this.currentUser = null;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.checkServerStatus();
        this.checkAuthStatus();
        this.loadGameStatistics();
        // Проверяем статус сервера каждые 30 секунд
        setInterval(() => this.checkServerStatus(), 30000);
        // Обновляем статистику каждые 15 секунд
        setInterval(() => this.loadGameStatistics(), 15000);
    }

    setupEventListeners() {
        // Табы авторизации
        document.getElementById('loginTab').addEventListener('click', () => this.showTab('login'));
        document.getElementById('registerTab').addEventListener('click', () => this.showTab('register'));

        // Формы авторизации
        document.getElementById('loginForm').addEventListener('submit', (e) => this.handleLogin(e));
        document.getElementById('registerForm').addEventListener('submit', (e) => this.handleRegister(e));

        // Профиль
        document.getElementById('saveProfileBtn').addEventListener('click', () => this.saveProfile());
        document.getElementById('playNowBtn').addEventListener('click', () => this.playGame());
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());
    }

    async checkServerStatus() {
        const statusDot = document.querySelector('.status-dot');
        const statusText = document.querySelector('.status-text');
        
        // Устанавливаем состояние "проверка"
        statusDot.className = 'status-dot checking';
        statusText.textContent = 'Проверка сервера...';

        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 секунд таймаут

            const response = await fetch('/api-game-player', {
                method: 'GET',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                statusDot.className = 'status-dot online';
                statusText.textContent = 'Сервер онлайн';
            } else {
                statusDot.className = 'status-dot offline';
                statusText.textContent = 'Сервер недоступен';
            }
        } catch (error) {
            if (error.name === 'AbortError') {
                statusDot.className = 'status-dot offline';
                statusText.textContent = 'Таймаут соединения';
            } else {
                statusDot.className = 'status-dot offline';
                statusText.textContent = 'Ошибка соединения';
            }
            console.error('Ошибка проверки сервера:', error);
        }
    }

    showTab(tabName) {
        // Переключение табов
        document.querySelectorAll('.tab').forEach(tab => tab.classList.remove('active'));
        document.querySelectorAll('.auth-form').forEach(form => form.classList.add('hidden'));

        if (tabName === 'login') {
            document.getElementById('loginTab').classList.add('active');
            document.getElementById('loginForm').classList.remove('hidden');
        } else {
            document.getElementById('registerTab').classList.add('active');
            document.getElementById('registerForm').classList.remove('hidden');
        }
    }

    async checkAuthStatus() {
        const userData = this.getStoredUser();
        if (userData) {
            try {
                // Проверяем, что пользователь существует на сервере
                const response = await fetch(`${this.baseUrl}/${userData.id}`, {
                    headers: {
                        'Cache-Control': 'no-cache',
                        'Pragma': 'no-cache'
                    }
                });
                if (response.ok) {
                    this.currentUser = userData;
                    this.showProfile();
                } else {
                    console.warn('Пользователь не найден на сервере, очищаем кеш');
                    this.clearStoredUser();
                    this.showAuth();
                }
            } catch (error) {
                console.error('Ошибка проверки авторизации:', error);
                this.showNotification('Ошибка подключения к серверу', 'error');
                this.clearStoredUser();
                this.showAuth();
            }
        } else {
            this.showAuth();
        }
    }

    async handleLogin(e) {
        e.preventDefault();
        const email = document.getElementById('loginEmail').value;
        const password = document.getElementById('loginPassword').value;

        this.showLoading(true);

        try {
            console.log('Попытка входа для:', email);
            
            const response = await fetch(`${this.baseUrl}/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Cache-Control': 'no-cache',
                    'Pragma': 'no-cache'
                },
                body: JSON.stringify({
                    email: email,
                    password: password
                })
            });
            
            console.log('Ответ сервера:', response.status, response.statusText);
            
            if (response.ok) {
                const user = await response.json();
                console.log('Пользователь авторизован:', user);
                
                this.currentUser = user;
                this.storeUser(user);
                this.showProfile();
                this.showNotification('Вход выполнен успешно!', 'success');
            } else {
                const errorMessage = await response.text();
                console.error('Ошибка авторизации:', errorMessage);
                this.showNotification(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Ошибка входа:', error);
            this.showNotification(`Ошибка соединения: ${error.message}`, 'error');
        } finally {
            this.showLoading(false);
        }
    }

    async handleRegister(e) {
        e.preventDefault();
        const email = document.getElementById('registerEmail').value;
        const password = document.getElementById('registerPassword').value;
        const nickname = document.getElementById('registerNick').value;
        const avatar = document.getElementById('registerAvatar').value;

        this.showLoading(true);

        try {
            console.log('Попытка регистрации для:', email);
            
            // Получаем количество пользователей для автоматического никнейма
            const usersResponse = await fetch(this.baseUrl);
            let playerNumber = 1;
            
            if (usersResponse.ok) {
                const users = await usersResponse.json();
                playerNumber = users.length + 1;
            }

            const userData = {
                username: nickname || `Player ${playerNumber}`,
                email: email,
                password: password,
                avatar: avatar || 'https://www.gravatar.com/avatar/?d=mp'
            };

            console.log('Отправка данных регистрации:', userData);

            const response = await fetch(this.baseUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(userData)
            });

            console.log('Ответ сервера:', response.status, response.statusText);

            if (response.ok) {
                const user = await response.json();
                console.log('Пользователь зарегистрирован:', user);
                
                this.currentUser = user;
                this.storeUser(user);
                this.showProfile();
                this.showNotification('Регистрация успешна!', 'success');
            } else {
                const errorMessage = await response.text();
                console.error('Ошибка регистрации:', errorMessage);
                this.showNotification(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Ошибка регистрации:', error);
            this.showNotification(`Ошибка соединения: ${error.message}`, 'error');
        } finally {
            this.showLoading(false);
        }
    }

    async saveProfile() {
        if (!this.currentUser) return;

        const newNick = document.getElementById('editNick').value;
        const newAvatar = document.getElementById('editAvatar').value;

        if (!newNick && !newAvatar) {
            this.showNotification('Введите новые данные', 'warning');
            return;
        }

        this.showLoading(true);

        try {
            const updateData = {};
            if (newNick) updateData.username = newNick;
            if (newAvatar) updateData.avatar = newAvatar;

            const response = await fetch(`${this.baseUrl}/${this.currentUser.id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updateData)
            });

            if (response.ok) {
                const updatedUser = await response.json();
                if (newAvatar) updatedUser.avatar = newAvatar;
                this.currentUser = updatedUser;
                this.storeUser(updatedUser);
                this.updateProfileDisplay();
                this.showNotification('Профиль обновлен!', 'success');
                
                // Очищаем поля ввода
                document.getElementById('editNick').value = '';
                document.getElementById('editAvatar').value = '';
            } else {
                const error = await response.text();
                this.showNotification(`Ошибка обновления: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Ошибка обновления профиля:', error);
            this.showNotification('Ошибка обновления профиля', 'error');
        } finally {
            this.showLoading(false);
        }
    }

    playGame() {
        if (!this.currentUser) return;

        this.showLoading(true);
        this.showNotification('Запуск игры...', 'info');

        setTimeout(() => {
            window.open(this.gameUrl, '_blank');
            this.showLoading(false);
        }, 2000);
    }

    logout() {
        this.clearStoredUser();
        this.currentUser = null;
        this.showAuth();
        this.showNotification('Вы вышли из системы', 'info');
    }

    showAuth() {
        document.getElementById('authSection').classList.remove('hidden');
        document.getElementById('profileSection').classList.add('hidden');
    }

    showProfile() {
        document.getElementById('authSection').classList.add('hidden');
        document.getElementById('profileSection').classList.remove('hidden');
        this.updateProfileDisplay();
    }

    updateProfileDisplay() {
        if (!this.currentUser) return;

        document.getElementById('profileNick').textContent = this.currentUser.username;
        document.getElementById('profileAvatar').src = this.currentUser.avatar || 'https://www.gravatar.com/avatar/?d=mp';
    }

    storeUser(user) {
        localStorage.setItem('secsUser', JSON.stringify(user));
    }

    getStoredUser() {
        const stored = localStorage.getItem('secsUser');
        return stored ? JSON.parse(stored) : null;
    }

    clearStoredUser() {
        localStorage.removeItem('secsUser');
    }

    showLoading(show) {
        const overlay = document.getElementById('loadingOverlay');
        if (show) {
            overlay.classList.remove('hidden');
        } else {
            overlay.classList.add('hidden');
        }
    }

    showNotification(message, type = 'info') {
        // Создаем элемент уведомления
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'error' ? 'rgba(255,0,0,0.9)' : 
                        type === 'success' ? 'rgba(0,255,0,0.9)' : 
                        type === 'warning' ? 'rgba(255,255,0,0.9)' : 'rgba(0,200,255,0.9)'};
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            font-family: 'Rajdhani', sans-serif;
            font-size: 1rem;
            z-index: 9999;
            opacity: 0;
            transform: translateX(100%);
            transition: all 0.3s ease;
        `;
        notification.textContent = message;

        document.body.appendChild(notification);

        // Анимация появления
        setTimeout(() => {
            notification.style.opacity = '1';
            notification.style.transform = 'translateX(0)';
        }, 100);

        // Автоматическое удаление
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }

    async loadGameStatistics() {
        try {
            // Показываем индикатор обновления
            const updateIndicator = document.getElementById('updateIndicator');
            if (updateIndicator) {
                updateIndicator.classList.add('updating');
            }

            // Получаем данные с сервера
            const response = await fetch('/api-game-statistics', {
                method: 'GET',
                headers: {
                    'Cache-Control': 'no-cache',
                    'Pragma': 'no-cache'
                }
            });

            if (response.ok) {
                const stats = await response.json();
                this.updateStatisticsDisplay(stats);
            } else {
                console.warn('Не удалось получить статистику игры');
                // Показываем заглушки
                this.updateStatisticsDisplay({
                    totalPlayers: '-',
                    onlinePlayers: '-',
                    totalMatches: '-',
                    liveMatches: '-'
                });
            }
        } catch (error) {
            console.error('Ошибка загрузки статистики:', error);
            // Показываем заглушки при ошибке
            this.updateStatisticsDisplay({
                totalPlayers: '-',
                onlinePlayers: '-',
                totalMatches: '-',
                liveMatches: '-'
            });
        } finally {
            // Убираем индикатор обновления
            const updateIndicator = document.getElementById('updateIndicator');
            if (updateIndicator) {
                updateIndicator.classList.remove('updating');
            }
        }
    }

    updateStatisticsDisplay(stats) {
        // Функция для анимированного обновления числа
        const animateNumber = (elementId, newValue) => {
            const element = document.getElementById(elementId);
            if (!element) return;

            const currentValue = element.textContent;
            if (currentValue !== newValue.toString()) {
                element.classList.add('updating');
                setTimeout(() => {
                    element.textContent = newValue;
                    element.classList.remove('updating');
                }, 250);
            }
        };

        // Обновляем все статистики
        animateNumber('totalPlayers', stats.totalPlayers);
        animateNumber('onlinePlayers', stats.onlinePlayers);
        animateNumber('totalMatches', stats.totalMatches);
        animateNumber('liveMatches', stats.liveMatches);

        // Обновляем время последнего обновления
        const lastUpdateElement = document.getElementById('lastUpdate');
        if (lastUpdateElement) {
            const now = new Date();
            const timeString = now.toLocaleTimeString('ru-RU', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
            lastUpdateElement.textContent = timeString;
        }
    }
}

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    new GameAuth();
});

// Добавляем обработчики для улучшения UX
document.addEventListener('DOMContentLoaded', () => {
    // Анимация звезд на фоне
    const stars = document.querySelector('.stars');
    if (stars) {
        let mouseX = 0;
        let mouseY = 0;
        
        document.addEventListener('mousemove', (e) => {
            mouseX = e.clientX;
            mouseY = e.clientY;
            
            const moveX = (mouseX - window.innerWidth / 2) * 0.01;
            const moveY = (mouseY - window.innerHeight / 2) * 0.01;
            
            stars.style.transform = `translate(${moveX}px, ${moveY}px)`;
        });
    }

    // Плавное появление элементов
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    });

    document.querySelectorAll('.info-card').forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'all 0.6s ease';
        observer.observe(card);
    });

    // Эффект набора текста для заголовка
    const title = document.querySelector('.game-title');
    if (title) {
        const text = title.textContent;
        title.textContent = '';
        title.style.borderRight = '2px solid #00c8ff';
        
        let i = 0;
        const typeWriter = () => {
            if (i < text.length) {
                title.textContent += text.charAt(i);
                i++;
                setTimeout(typeWriter, 150);
            } else {
                setTimeout(() => {
                    title.style.borderRight = 'none';
                }, 500);
            }
        };
        
        setTimeout(typeWriter, 1000);
    }
});
