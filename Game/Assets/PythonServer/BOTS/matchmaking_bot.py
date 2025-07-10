import requests
import json
import time
import random
import threading
from datetime import datetime

class MatchmakingBot:
    def __init__(self, bot_id, bot_name, base_url="https://renderfin.com"):
        self.bot_id = bot_id
        self.bot_name = bot_name
        self.base_url = base_url
        self.in_queue = False
        self.in_match = False
        self.current_match_id = None
        self.session = requests.Session()
        
        # Настройки поведения бота
        self.queue_join_interval = random.randint(15, 45)  # 15-45 секунд между попытками
        self.match_types = [1, 2, 4]  # OneVsOne, TwoVsTwo, FourPlayerFFA
        
    def log(self, message):
        timestamp = datetime.now().strftime("%H:%M:%S")
        print(f"[{timestamp}] Bot-{self.bot_id} ({self.bot_name}): {message}")
    
    def join_queue(self):
        """Присоединяется к очереди матчмейкинга"""
        if self.in_queue or self.in_match:
            return False
            
        try:
            match_type = random.choice(self.match_types)
            response = self.session.post(
                f"{self.base_url}/api-game-queue/{self.bot_id}/join",
                json={"matchType": match_type},
                timeout=10
            )
            
            if response.status_code == 200:
                self.in_queue = True
                self.log(f"Присоединился к очереди (тип: {match_type})")
                return True
            else:
                self.log(f"Ошибка при присоединении к очереди: {response.status_code}")
                return False
                
        except Exception as e:
            self.log(f"Ошибка при присоединении к очереди: {e}")
            return False
    
    def leave_queue(self):
        """Покидает очередь матчмейкинга"""
        if not self.in_queue:
            return False
            
        try:
            response = self.session.post(
                f"{self.base_url}/api-game-queue/{self.bot_id}/leave",
                timeout=10
            )
            
            if response.status_code == 200:
                self.in_queue = False
                self.log("Покинул очередь")
                return True
            else:
                self.log(f"Ошибка при выходе из очереди: {response.status_code}")
                return False
                
        except Exception as e:
            self.log(f"Ошибка при выходе из очереди: {e}")
            return False
    
    def check_queue_status(self):
        """Проверяет статус в очереди"""
        try:
            response = self.session.get(
                f"{self.base_url}/api-game-queue/{self.bot_id}/status",
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('inQueue', False):
                    queue_time = data.get('queueTime', 0)
                    if queue_time > 0:
                        self.log(f"В очереди {queue_time} секунд")
                else:
                    self.in_queue = False
                return data
            else:
                return None
                
        except Exception as e:
            self.log(f"Ошибка при проверке статуса очереди: {e}")
            return None
    
    def check_match_status(self):
        """Проверяет, не начался ли матч"""
        try:
            response = self.session.get(
                f"{self.base_url}/api-game-player/{self.bot_id}",
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                current_match = data.get('currentMatchId')
                
                if current_match and not self.in_match:
                    self.current_match_id = current_match
                    self.in_match = True
                    self.in_queue = False
                    self.log(f"Начался матч #{current_match}")
                    
                    # Запускаем обработку матча в отдельном потоке
                    threading.Thread(target=self.handle_match, daemon=True).start()
                    
                elif not current_match and self.in_match:
                    self.in_match = False
                    self.current_match_id = None
                    self.log("Матч завершен")
                    
                return data
            else:
                return None
                
        except Exception as e:
            self.log(f"Ошибка при проверке статуса матча: {e}")
            return None
    
    def handle_match(self):
        """Обрабатывает матч (имитирует игру)"""
        if not self.current_match_id:
            return
            
        self.log(f"Играю в матче #{self.current_match_id}")
        
        # Имитируем игру (ждем 30-60 секунд)
        game_duration = random.randint(30, 60)
        time.sleep(game_duration)
        
        # Проверяем, не завершился ли матч автоматически
        if self.in_match:
            self.finish_match()
    
    def finish_match(self):
        """Завершает матч (имитирует победу/поражение)"""
        if not self.current_match_id or not self.in_match:
            return
            
        try:
            # Случайно определяем победителя/проигравшего
            is_winner = random.choice([True, False])
            
            if is_winner:
                winners = [self.bot_id]
                losers = []  # Других игроков определит сервер
            else:
                winners = []
                losers = [self.bot_id]
            
            response = self.session.post(
                f"{self.base_url}/api-game-match/{self.current_match_id}/finish",
                json={
                    "winners": winners,
                    "losers": losers
                },
                timeout=10
            )
            
            if response.status_code == 200:
                result = "выиграл" if is_winner else "проиграл"
                self.log(f"Матч #{self.current_match_id} завершен - {result}")
            else:
                self.log(f"Ошибка при завершении матча: {response.status_code}")
                
        except Exception as e:
            self.log(f"Ошибка при завершении матча: {e}")
    
    def send_heartbeat(self):
        """Отправляет heartbeat для поддержания онлайн статуса"""
        try:
            response = self.session.post(
                f"{self.base_url}/api-game-player/heartbeat",
                json={"userId": self.bot_id},
                timeout=10
            )
            
            if response.status_code != 200:
                self.log(f"Ошибка heartbeat: {response.status_code}")
                
        except Exception as e:
            self.log(f"Ошибка heartbeat: {e}")
    
    def run(self):
        """Основной цикл работы бота"""
        self.log("Бот запущен")
        
        last_heartbeat = 0
        last_queue_attempt = 0
        
        while True:
            try:
                current_time = time.time()
                
                # Отправляем heartbeat каждые 60 секунд
                if current_time - last_heartbeat >= 60:
                    self.send_heartbeat()
                    last_heartbeat = current_time
                
                # Проверяем статус матча
                self.check_match_status()
                
                # Если не в матче и не в очереди, пытаемся присоединиться к очереди
                if not self.in_match and not self.in_queue:
                    if current_time - last_queue_attempt >= self.queue_join_interval:
                        if self.join_queue():
                            last_queue_attempt = current_time
                            # Случайная задержка перед следующей попыткой
                            self.queue_join_interval = random.randint(15, 45)
                
                # Проверяем статус очереди
                if self.in_queue:
                    self.check_queue_status()
                
                # Пауза между итерациями
                time.sleep(5)
                
            except KeyboardInterrupt:
                self.log("Остановка бота...")
                break
            except Exception as e:
                self.log(f"Неожиданная ошибка: {e}")
                time.sleep(10)
        
        # Очистка при выходе
        if self.in_queue:
            self.leave_queue()
        
        self.log("Бот остановлен")

def main():
    # Загружаем список ботов
    try:
        with open('bots_list.txt', 'r', encoding='utf-8') as f:
            bots = []
            for line in f:
                line = line.strip()
                if line and not line.startswith('#'):
                    parts = line.split(',')
                    if len(parts) >= 3:
                        bot_id = int(parts[0])
                        bot_name = parts[1]
                        bots.append((bot_id, bot_name))
    except FileNotFoundError:
        print("Файл bots_list.txt не найден")
        return
    
    # Запускаем ботов (только первые 50 для начала)
    active_bots = bots[:50]
    threads = []
    
    for bot_id, bot_name in active_bots:
        bot = MatchmakingBot(bot_id, bot_name)
        thread = threading.Thread(target=bot.run, daemon=True)
        threads.append(thread)
        thread.start()
        
        # Небольшая задержка между запусками
        time.sleep(0.1)
    
    print(f"Запущено {len(active_bots)} ботов для матчмейкинга")
    
    try:
        # Ждем завершения всех потоков
        for thread in threads:
            thread.join()
    except KeyboardInterrupt:
        print("Остановка всех ботов...")

if __name__ == "__main__":
    main() 