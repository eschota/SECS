# bot_manager.py
#
# Система ботов для тестирования игровой инфраструктуры
# Работает напрямую с API https://renderfin.com/api-game-user/
# Работает напрямую с API https://renderfin.com/api-game-queue/
# Работает напрямую с API https://renderfin.com/api-game-match/
#
# Необходимо создать столько ботов, сколько указано в bots_count.
# Боты имеют отрицательные player_id для отличия от реальных игроков.

import requests
import json
import time
import random
import threading
from datetime import datetime, timedelta
from typing import Dict, List, Optional
import sqlite3
import os
import sys

# Добавляем родительскую папку в путь для импорта
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Настройки
API_BASE_URL = "https://renderfin.com"
ADMIN_TOKEN = "ZXCVBNM,1234567890"
bots_count = 50  # Количество ботов для матчмейкинга

# Заголовки для запросов
HEADERS = {
    'Content-Type': 'application/json',
    'Authorization': f'Bearer {ADMIN_TOKEN}'
}

class Bot:
    """Класс для управления ботом"""
    
    def __init__(self, bot_id: str, nick_name: str, email: str, password: str):
        self.bot_id = bot_id
        self.nick_name = nick_name
        self.email = email
        self.password = password
        self.is_active = True
        self.last_action_time = datetime.now()
        self.current_state = "idle"  # idle, in_queue, in_match
        self.current_match_id = None
        self.current_queue_ticket_id = None
        self.preferred_match_types = [1, 2, 4]  # OneVsOne, TwoVsTwo, FourPlayerFFA
        self.mmr_one_vs_one = 0
        self.mmr_two_vs_two = 0
        self.mmr_four_player_ffa = 0
        self.last_heartbeat = datetime.now()
        self.queue_join_interval = random.randint(15, 45)  # Случайная задержка между попытками входа в очередь
        self.session = requests.Session()  # Сессия для HTTP запросов
        
    def __str__(self):
        return f"Bot({self.bot_id}, {self.nick_name}, {self.current_state})"

class BotManager:
    """Менеджер ботов"""
    
    def __init__(self):
        self.bots: Dict[str, Bot] = {}
        self.bots_db_path = "bots_database.db"
        self.running = False
        self.threads = []
        self.stats = {
            'total_bots': 0,
            'active_bots': 0,
            'bots_in_queue': 0,
            'bots_in_match': 0,
            'matches_played': 0,
            'queue_attempts': 0,
            'api_errors': 0
        }
        self.init_bots_database()
        
    def init_bots_database(self):
        """Инициализация базы данных ботов"""
        conn = sqlite3.connect(self.bots_db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS bots_list (
                bot_id TEXT PRIMARY KEY,
                nick_name TEXT NOT NULL,
                email TEXT NOT NULL,
                password TEXT NOT NULL,
                matches_played INTEGER DEFAULT 0,
                wins INTEGER DEFAULT 0,
                losses INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_active TIMESTAMP
            )
        ''')
        
        conn.commit()
        conn.close()
        
    def save_bot_to_db(self, bot: Bot):
        """Сохранить бота в базу данных"""
        conn = sqlite3.connect(self.bots_db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT OR REPLACE INTO bots_list 
            (bot_id, nick_name, email, password, last_active)
            VALUES (?, ?, ?, ?, ?)
        ''', (
            bot.bot_id,
            bot.nick_name,
            bot.email,
            bot.password,
            datetime.now().isoformat()
        ))
        
        conn.commit()
        conn.close()
        
    def load_bots_from_db(self):
        """Загрузить ботов из базы данных"""
        conn = sqlite3.connect(self.bots_db_path)
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM bots_list')
        rows = cursor.fetchall()
        
        for row in rows:
            bot_id, nick_name, email, password, matches_played, wins, losses, created_at, last_active = row
            
            bot = Bot(bot_id, nick_name, email, password)
            self.bots[bot_id] = bot
            
        conn.close()
        print(f"Loaded {len(self.bots)} bots from database")
        
    def create_bots(self):
        """Создает ботов с именами bot_-{player_id}"""
        print(f"Creating {bots_count} bots...")
        
        for i in range(1, bots_count + 1):
            bot_id = f"bot_-{i}"  # Временный ID для создания
            nick_name = f"Bot_Player_{i}"
            email = f"bot{i}@testbot.com"
            password = f"botpass{i}"
            
            # Создаем бота
            bot = Bot(bot_id, nick_name, email, password)
            
            # Регистрируем в игровой системе и получаем реальный ID
            real_player_id = self.register_bot_in_game(bot)
            if real_player_id:
                # Обновляем ID бота на реальный, полученный от сервера
                bot.bot_id = real_player_id
                self.bots[real_player_id] = bot
                self.save_bot_to_db(bot)
                print(f"Created bot: {bot} (real ID: {real_player_id})")
            else:
                print(f"Failed to create bot: {bot_id}")
                
            # Небольшая задержка между созданием ботов
            time.sleep(0.1)
            
        print(f"Successfully created {len(self.bots)} bots")
        
    def register_bot_in_game(self, bot: Bot) -> Optional[str]:
        """Регистрирует бота в игровой системе и возвращает реальный player_id"""
        try:
            # Регистрируем в системе пользователей
            response = bot.session.post(
                f"{API_BASE_URL}/api-game-player/register",
                json={
                    "username": bot.nick_name,
                    "email": bot.email,
                    "password": bot.password
                },
                headers={'Content-Type': 'application/json'},
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    # Возвращаем реальный player_id, присвоенный сервером
                    real_player_id = str(data.get('user', {}).get('id'))
                    print(f"Bot registered successfully: {bot.nick_name} -> {real_player_id}")
                    return real_player_id
                else:
                    print(f"Registration failed for {bot.bot_id}: {data.get('message', 'Unknown error')}")
                    return None
            elif response.status_code == 400:
                print(f"Bot {bot.bot_id} might already exist - trying to login")
                # Пытаемся войти в систему
                login_response = bot.session.post(
                    f"{API_BASE_URL}/api-game-player/login",
                    json={
                        "email": bot.email,
                        "password": bot.password
                    },
                    headers={'Content-Type': 'application/json'},
                    timeout=10
                )
                
                if login_response.status_code == 200:
                    login_data = login_response.json()
                    if login_data.get('success'):
                        real_player_id = str(login_data.get('user', {}).get('id'))
                        print(f"Bot logged in successfully: {bot.nick_name} -> {real_player_id}")
                        return real_player_id
                        
                print(f"Failed to login bot {bot.bot_id}")
                return None
            else:
                print(f"HTTP error {response.status_code} for {bot.bot_id}")
                return None
                
        except Exception as e:
            print(f"Error registering bot {bot.bot_id}: {e}")
            return None
            
    def bot_action_cycle(self, bot: Bot):
        """Цикл действий бота"""
        last_heartbeat = 0
        last_queue_attempt = 0
        
        while self.running and bot.is_active:
            try:
                current_time = time.time()
                
                # Отправляем heartbeat каждые 60 секунд
                if current_time - last_heartbeat >= 60:
                    self.send_heartbeat(bot)
                    last_heartbeat = current_time
                
                # Проверяем состояние бота
                self.check_bot_state(bot)
                
                # Выполняем действия в зависимости от состояния
                if bot.current_state == "idle":
                    # Пытаемся присоединиться к очереди с интервалом
                    if current_time - last_queue_attempt >= bot.queue_join_interval:
                        if self.try_join_queue(bot):
                            last_queue_attempt = current_time
                            # Обновляем интервал
                            bot.queue_join_interval = random.randint(15, 45)
                        
                elif bot.current_state == "in_queue":
                    # Проверяем статус очереди
                    self.check_queue_status(bot)
                    
                elif bot.current_state == "in_match":
                    # Проверяем статус матча и обрабатываем игру
                    self.handle_match_actions(bot)
                    
                # Обновляем время последнего действия
                bot.last_action_time = datetime.now()
                
            except Exception as e:
                print(f"Error in bot {bot.bot_id} cycle: {e}")
                self.stats['api_errors'] += 1
                
            # Пауза между действиями
            time.sleep(5)
            
    def check_bot_state(self, bot: Bot):
        """Проверяет текущее состояние бота через сервер"""
        try:
            # Получаем информацию о боте
            response = bot.session.get(
                f"{API_BASE_URL}/api-game-player/{bot.bot_id}",
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    user_data = data.get('user', {})
                    
                    # Проверяем состояние: в матче или нет
                    current_match_id = user_data.get('currentMatchId')
                    is_in_queue = user_data.get('isInQueue', False)
                    
                    if current_match_id:
                        if bot.current_state != "in_match":
                            print(f"Bot {bot.bot_id} entered match #{current_match_id}")
                        bot.current_state = "in_match"
                        bot.current_match_id = current_match_id
                        bot.current_queue_ticket_id = None
                    elif is_in_queue:
                        if bot.current_state != "in_queue":
                            print(f"Bot {bot.bot_id} is in queue")
                        bot.current_state = "in_queue"
                        bot.current_match_id = None
                    else:
                        if bot.current_state != "idle":
                            print(f"Bot {bot.bot_id} is now idle")
                        bot.current_state = "idle"
                        bot.current_match_id = None
                        bot.current_queue_ticket_id = None
                        
        except Exception as e:
            print(f"Error checking bot state for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            
    def try_join_queue(self, bot: Bot):
        """Пытается добавить бота в очередь"""
        try:
            # Выбираем случайный тип матча из предпочитаемых
            match_type = random.choice(bot.preferred_match_types)
            
            response = bot.session.post(
                f"{API_BASE_URL}/api-game-queue/{bot.bot_id}/join",
                json={
                    "matchType": match_type
                },
                headers={'Content-Type': 'application/json'},
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                bot.current_state = "in_queue"
                print(f"Bot {bot.bot_id} joined queue for match type {match_type}")
                self.stats['queue_attempts'] += 1
                return True
            else:
                print(f"Failed to join queue for {bot.bot_id}: HTTP {response.status_code}")
                return False
                    
        except Exception as e:
            print(f"Error joining queue for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            return False
            
    def check_queue_status(self, bot: Bot):
        """Проверяет статус бота в очереди"""
        try:
            response = bot.session.get(
                f"{API_BASE_URL}/api-game-queue/{bot.bot_id}/status",
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if not data.get('inQueue', False):
                    # Бот больше не в очереди, проверяем не в матче ли он
                    self.check_bot_state(bot)
                else:
                    # Показываем время в очереди
                    queue_time = data.get('queueTime', 0)
                    if queue_time > 30:  # Показываем только если больше 30 секунд
                        queue_type = data.get('queueType', 'unknown')
                        print(f"Bot {bot.bot_id} in queue for {queue_time}s (type: {queue_type})")
                        
        except Exception as e:
            print(f"Error checking queue status for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            
    def handle_match_actions(self, bot: Bot):
        """Обрабатывает действия бота в матче"""
        try:
            if not bot.current_match_id:
                return
                
            # Получаем информацию о матче
            response = bot.session.get(
                f"{API_BASE_URL}/api-game-match/{bot.current_match_id}/status",
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                match_status = data.get('status')
                elapsed_time = data.get('elapsedTime', 0)
                
                # Если матч активен (InProgress), имитируем игру
                if match_status == 0:  # GameMatchStatus.InProgress
                    # Имитируем игру некоторое время, потом пытаемся завершить
                    if elapsed_time > 30:  # Играем минимум 30 секунд
                        if random.random() < 0.1:  # 10% шанс попытаться завершить матч
                            self.try_finish_match(bot)
                            
                # Если матч завершен, обновляем состояние
                elif match_status in [1, 2]:  # Completed или Cancelled
                    bot.current_state = "idle"
                    old_match_id = bot.current_match_id
                    bot.current_match_id = None
                    self.stats['matches_played'] += 1
                    print(f"Bot {bot.bot_id} finished match #{old_match_id}")
                        
        except Exception as e:
            print(f"Error handling match actions for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            
    def try_finish_match(self, bot: Bot):
        """Пытается завершить матч (имитирует окончание игры)"""
        try:
            if not bot.current_match_id:
                return
                
            # Случайно определяем, выиграл ли бот
            is_winner = random.choice([True, False])
            
            if is_winner:
                winners = [int(bot.bot_id)]
                losers = []  # Других игроков определит сервер
            else:
                winners = []
                losers = [int(bot.bot_id)]
            
            response = bot.session.post(
                f"{API_BASE_URL}/api-game-match/{bot.current_match_id}/finish",
                json={
                    "winners": winners,
                    "losers": losers
                },
                headers={'Content-Type': 'application/json'},
                timeout=10
            )
            
            if response.status_code == 200:
                result = "выиграл" if is_winner else "проиграл"
                print(f"Bot {bot.bot_id} finished match #{bot.current_match_id} - {result}")
            else:
                print(f"Failed to finish match for bot {bot.bot_id}: HTTP {response.status_code}")
                
        except Exception as e:
            print(f"Error finishing match for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            
    def send_heartbeat(self, bot: Bot):
        """Отправляет heartbeat для поддержания онлайн статуса"""
        try:
            response = bot.session.post(
                f"{API_BASE_URL}/api-game-player/heartbeat",
                json={"userId": int(bot.bot_id)},
                headers={'Content-Type': 'application/json'},
                timeout=10
            )
            
            if response.status_code == 200:
                bot.last_heartbeat = datetime.now()
            else:
                print(f"Heartbeat failed for bot {bot.bot_id}: HTTP {response.status_code}")
                
        except Exception as e:
            print(f"Error sending heartbeat for {bot.bot_id}: {e}")
            self.stats['api_errors'] += 1
            
    def update_stats(self):
        """Обновляет статистику ботов"""
        self.stats['total_bots'] = len(self.bots)
        self.stats['active_bots'] = sum(1 for bot in self.bots.values() if bot.is_active)
        self.stats['bots_in_queue'] = sum(1 for bot in self.bots.values() if bot.current_state == "in_queue")
        self.stats['bots_in_match'] = sum(1 for bot in self.bots.values() if bot.current_state == "in_match")
        
    def print_stats(self):
        """Выводит статистику работы ботов"""
        self.update_stats()
        print("\n" + "="*50)
        print("BOT MANAGER STATISTICS")
        print("="*50)
        print(f"Total bots: {self.stats['total_bots']}")
        print(f"Active bots: {self.stats['active_bots']}")
        print(f"Bots in queue: {self.stats['bots_in_queue']}")
        print(f"Bots in match: {self.stats['bots_in_match']}")
        print(f"Matches played: {self.stats['matches_played']}")
        print(f"Queue attempts: {self.stats['queue_attempts']}")
        print(f"API errors: {self.stats['api_errors']}")
        print("="*50)
        
        # Детализированная информация о ботах
        print("\nBOT STATES:")
        for bot_id, bot in list(self.bots.items())[:10]:  # Показываем первых 10 ботов
            print(f"{bot_id}: {bot.current_state}")
            
    def start(self):
        """Запускает систему ботов"""
        print("Starting Bot Manager...")
        
        # Загружаем существующих ботов
        self.load_bots_from_db()
        
        # Создаем новых ботов если нужно
        if len(self.bots) < bots_count:
            self.create_bots()
            
        self.running = True
        
        # Запускаем потоки для каждого бота
        for bot in self.bots.values():
            thread = threading.Thread(target=self.bot_action_cycle, args=(bot,), daemon=True)
            thread.start()
            self.threads.append(thread)
            
        # Запускаем поток статистики
        stats_thread = threading.Thread(target=self.stats_loop, daemon=True)
        stats_thread.start()
        
        print(f"Bot Manager started with {len(self.bots)} bots")
        
    def stats_loop(self):
        """Цикл вывода статистики"""
        while self.running:
            time.sleep(30)  # Каждые 30 секунд
            self.print_stats()
            
    def stop(self):
        """Останавливает систему ботов"""
        print("Stopping Bot Manager...")
        self.running = False
        
        # Удаляем всех ботов из очередей
        for bot in self.bots.values():
            try:
                requests.delete(
                    f"{API_BASE_URL}/api-game-queue/",
                    json={"player_id": bot.bot_id},
                    headers={'Content-Type': 'application/json'},
                    timeout=5
                )
            except:
                pass
                
        print("Bot Manager stopped")

# Глобальный экземпляр менеджера ботов
bot_manager = BotManager()

def main():
    """Основная функция для запуска системы ботов"""
    try:
        bot_manager.start()
        
        print("\nBot Manager is running!")
        print("Press Ctrl+C to stop...")
        
        # Ждем сигнала остановки
        while True:
            time.sleep(1)
            
    except KeyboardInterrupt:
        print("\nReceived stop signal...")
        bot_manager.stop()
        print("Bot Manager stopped successfully!")

if __name__ == "__main__":
    main()





