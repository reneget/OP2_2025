// Конфигурация - будет установлена из /config.js перед загрузкой этого скрипта
const SERVER_URL = window.SERVER_URL || 'http://localhost:5247';

// Состояние приложения
let currentUser = null;

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    initializeTabs();
    initializeForms();
    checkAuthStatus();
});

// Проверка статуса авторизации
async function checkAuthStatus() {
    try {
        const response = await fetch(`${SERVER_URL}/api/check_user`, {
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            currentUser = data.username;
            showMainScreen(data.username);
        } else {
            showAuthScreen();
        }
    } catch (error) {
        console.error('Ошибка проверки авторизации:', error);
        showAuthScreen();
    }
}

// Переключение табов авторизации
function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const authForms = document.querySelectorAll('.auth-form');

    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabName = btn.dataset.tab;

            // Обновить активные табы
            tabButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            // Обновить активные формы
            authForms.forEach(f => {
                f.classList.remove('active');
                if (f.id === `${tabName}-form`) {
                    f.classList.add('active');
                }
            });

            // Очистить сообщения
            clearMessages();
        });
    });
}

// Инициализация форм
function initializeForms() {
    // Форма входа
    document.getElementById('loginForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleLogin();
    });

    // Форма регистрации
    document.getElementById('signupForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleSignup();
    });

    // Форма сортировки
    document.getElementById('sort-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleSort();
    });

    // Кнопка загрузки логов
    document.getElementById('load-logs-btn').addEventListener('click', async () => {
        await loadLogs();
    });

    // Кнопка выхода
    document.getElementById('logout-btn').addEventListener('click', () => {
        currentUser = null;
        showAuthScreen();
    });
}

// Обработка входа
async function handleLogin() {
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const errorDiv = document.getElementById('login-error');

    clearMessages();

    try {
        const response = await fetch(`${SERVER_URL}/api/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({
                login: username,
                password: password
            })
        });

        if (response.ok) {
            const data = await response.json();
            currentUser = data.username;
            showMainScreen(data.username);
            document.getElementById('loginForm').reset();
        } else {
            showError(errorDiv, 'Неверный логин или пароль');
        }
    } catch (error) {
        console.error('Ошибка входа:', error);
        showError(errorDiv, 'Ошибка подключения к серверу');
    }
}

// Обработка регистрации
async function handleSignup() {
    const username = document.getElementById('signup-username').value;
    const password = document.getElementById('signup-password').value;
    const errorDiv = document.getElementById('signup-error');
    const successDiv = document.getElementById('signup-success');

    clearMessages();

    try {
        const response = await fetch(`${SERVER_URL}/api/signup`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({
                login: username,
                password: password
            })
        });

        if (response.ok) {
            const data = await response.json();
            showSuccess(successDiv, data.message);
            document.getElementById('signupForm').reset();
            
            // Переключить на вкладку входа через 2 секунды
            setTimeout(() => {
                document.querySelector('[data-tab="login"]').click();
                document.getElementById('login-username').value = username;
            }, 2000);
        } else {
            const errorData = await response.json();
            showError(errorDiv, errorData.error || 'Ошибка регистрации');
        }
    } catch (error) {
        console.error('Ошибка регистрации:', error);
        showError(errorDiv, 'Ошибка подключения к серверу');
    }
}

// Обработка сортировки
async function handleSort() {
    const arrayInput = document.getElementById('array-input').value;
    const direction = document.querySelector('input[name="sort-direction"]:checked').value;
    const resultDiv = document.getElementById('sort-result');

    clearResult(resultDiv);

    try {
        // Парсинг массива
        const numbers = arrayInput.trim().split(/\s+/).map(s => parseInt(s.trim()));
        
        if (numbers.some(isNaN)) {
            showResult(resultDiv, 'Ошибка: Введите только целые числа, разделённые пробелами', 'error');
            return;
        }

        if (numbers.length === 0) {
            showResult(resultDiv, 'Ошибка: Массив не может быть пустым', 'error');
            return;
        }

        // Показать загрузку
        resultDiv.innerHTML = '<div class="loading"><div class="spinner"></div><p>Сортировка...</p></div>';
        resultDiv.classList.add('show');

        const response = await fetch(`${SERVER_URL}/api/sort`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({
                array: numbers,
                ascending: direction === 'ascending'
            })
        });

        if (response.ok) {
            const data = await response.json();
            displaySortResult(resultDiv, data);
        } else {
            const errorData = await response.json();
            showResult(resultDiv, errorData.error || 'Ошибка сортировки', 'error');
        }
    } catch (error) {
        console.error('Ошибка сортировки:', error);
        showResult(resultDiv, 'Ошибка подключения к серверу', 'error');
    }
}

// Отображение результата сортировки
function displaySortResult(container, data) {
    container.innerHTML = `
        <div class="result-item">
            <div class="result-label">Исходный массив:</div>
            <div class="result-value original">[${data.originalArray.join(', ')}]</div>
        </div>
        <div class="result-item">
            <div class="result-label">Отсортированный массив:</div>
            <div class="result-value sorted">[${data.sortedArray.join(', ')}]</div>
        </div>
        <div class="result-item">
            <div class="result-label">Направление:</div>
            <div class="result-value">${data.ascending ? 'По возрастанию' : 'По убыванию'}</div>
        </div>
    `;
    container.classList.add('show');
}

// Загрузка логов
async function loadLogs() {
    const period = document.getElementById('log-period').value;
    const logsContainer = document.getElementById('logs-container');

    logsContainer.innerHTML = '<div class="loading"><div class="spinner"></div><p>Загрузка логов...</p></div>';

    try {
        let from = null;
        let to = null;

        if (period !== 'all') {
            const days = parseInt(period);
            from = new Date(Date.now() - days * 24 * 60 * 60 * 1000);
            to = new Date();
        }

        const params = new URLSearchParams();
        if (from) params.append('from', from.toISOString());
        if (to) params.append('to', to.toISOString());

        const url = `${SERVER_URL}/api/logs${params.toString() ? '?' + params.toString() : ''}`;
        
        const response = await fetch(url, {
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            displayLogs(logsContainer, data.logs || []);
        } else {
            logsContainer.innerHTML = '<div class="logs-empty">Ошибка загрузки логов</div>';
        }
    } catch (error) {
        console.error('Ошибка загрузки логов:', error);
        logsContainer.innerHTML = '<div class="logs-empty">Ошибка подключения к серверу</div>';
    }
}

// Отображение логов
function displayLogs(container, logs) {
    if (logs.length === 0) {
        container.innerHTML = '<div class="logs-empty">Логи не найдены</div>';
        return;
    }

    container.innerHTML = logs.map(log => {
        let arraysInfo = '';
        if (log.inputArray && log.outputArray) {
            arraysInfo = `
                <div class="log-arrays">
                    <div class="log-array-item">
                        <span class="log-array-label">Входной:</span>
                        <span class="log-array-value">[${log.inputArray.join(', ')}]</span>
                    </div>
                    <div class="log-array-item">
                        <span class="log-array-label">Выходной:</span>
                        <span class="log-array-value sorted">[${log.outputArray.join(', ')}]</span>
                    </div>
                </div>
            `;
        }
        
        return `
            <div class="log-entry">
                <div class="log-header">
                    <span class="log-timestamp">${formatDate(log.timestamp)}</span>
                    <span class="log-level ${log.level}">${log.level}</span>
                    ${log.userId ? `<span class="log-user">[${log.userId}]</span>` : ''}
                </div>
                <div class="log-message">${escapeHtml(log.message)}</div>
                ${arraysInfo}
            </div>
        `;
    }).join('');
}

// Форматирование даты
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}

// Экранирование HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Утилиты
function showAuthScreen() {
    document.getElementById('auth-screen').classList.add('active');
    document.getElementById('main-screen').classList.remove('active');
}

function showMainScreen(username) {
    document.getElementById('auth-screen').classList.remove('active');
    document.getElementById('main-screen').classList.add('active');
    document.getElementById('username-display').textContent = username;
}

function clearMessages() {
    document.querySelectorAll('.error-message, .success-message').forEach(el => {
        el.classList.remove('show');
        el.textContent = '';
    });
}

function showError(element, message) {
    element.textContent = message;
    element.classList.add('show');
}

function showSuccess(element, message) {
    element.textContent = message;
    element.classList.add('show');
}

function clearResult(container) {
    container.innerHTML = '';
    container.classList.remove('show');
}

function showResult(container, message, type = 'error') {
    container.innerHTML = `<div class="${type === 'error' ? 'error-message' : 'success-message'} show">${escapeHtml(message)}</div>`;
    container.classList.add('show');
}

