#!/usr/bin/env python3
# -*- coding: utf-8 -*-

#c:\SECS\Server\Bots\start_bot_manager.bat - используй абсолютный путь для запуска.
"""
🎮 SECS Unified Bot Manager - Единый менеджер ботов для SECS
Включает создание, регистрацию, heartbeat, матчмейкинг и статистику
"""

import requests
import json
import os
import random
import string
import time
import logging
from datetime import datetime, timedelta
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Dict, List, Optional, Tuple
import threading
from enum import Enum

class BotBehavior(Enum):
    """🎯 Типы поведения ботов"""
    AGGRESSIVE = "aggressive"     # Часто ищет матчи
    CASUAL = "casual"            # Иногда ищет матчи
    PASSIVE = "passive"          # Редко ищет матчи
    RANDOM = "random"            # Случайное поведение

class MatchType(Enum):
    """🎲 Типы матчей"""
    ONE_VS_ONE = 1
    TWO_VS_TWO = 2
    FOUR_PLAYER_FFA = 4

class UnifiedBotsManager:
    """🎮 Единый менеджер ботов для SECS"""
    
    def __init__(self):
        self.base_url = "https://renderfin.com"
        self.register_endpoint = f"{self.base_url}/api-game-player"
        self.login_endpoint = f"{self.base_url}/api-game-player/login"
        self.queue_endpoint = f"{self.base_url}/api-game-queue"
        self.match_endpoint = f"{self.base_url}/api-game-match"
        
        self.bots_data_dir = "bots_data"
        self.bots_list_file = os.path.join(self.bots_data_dir, "bots_list.json")
        
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'User-Agent': 'SECS-Unified-Bot-Manager/1.0'
        })
        
        # Увеличиваем размер пула соединений для поддержки тысяч игроков
        from requests.adapters import HTTPAdapter
        from urllib3.util.retry import Retry
        
        retry_strategy = Retry(
            total=3,
            backoff_factor=0.1,
            status_forcelist=[429, 500, 502, 503, 504],
        )
        
        adapter = HTTPAdapter(
            pool_connections=50,  # Количество пулов соединений
            pool_maxsize=100,     # Максимальное количество соединений в пуле
            max_retries=retry_strategy
        )
        
        self.session.mount("http://", adapter)
        self.session.mount("https://", adapter)
        
        os.makedirs(self.bots_data_dir, exist_ok=True)
        
        # Список космических имен для ботов
        self.space_names = [
            "Nebula", "Plasma", "Quantum", "Void", "Stellar", "Cosmic", "Galactic", "Nova",
            "Pulsar", "Quasar", "Meteor", "Asteroid", "Comet", "Eclipse", "Photon", "Proton",
            "Neutron", "Electron", "Vector", "Matrix", "Cipher", "Ghost", "Shadow", "Phantom",
            "Vortex", "Helix", "Prism", "Fusion", "Reactor", "Catalyst", "Orbit", "Horizon",
            "Zenith", "Apex", "Vertex", "Nexus", "Flux", "Pulse", "Surge", "Spark",
            "Blaze", "Flare", "Storm", "Thunder", "Lightning", "Frost", "Ice", "Fire",
            "Inferno", "Magma", "Lava", "Crystal", "Diamond", "Steel", "Titanium", "Chrome",
            "Neon", "Laser", "Beam", "Ray", "Wave", "Signal", "Code", "Data",
            "Byte", "Pixel", "Digital", "Cyber", "Tech", "Proto", "Alpha", "Beta",
            "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa",
            "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma",
            "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega", "Prime", "Zero",
            "One", "Binary", "Hex", "Core", "Node", "Link", "Grid", "Net"
        ]
        
        self.space_suffixes = [
            "X", "Z", "Prime", "Neo", "Ultra", "Max", "Pro", "Elite", "Master", "Lord",
            "2023", "2024", "V2", "V3", "Plus", "Extreme", "Force", "Power", "Titan", "King"
        ]
        
        # Загружаем существующих ботов
        self.bots_data = self.load_bots_data()
        
        # Блокировка для потокобезопасности
        self.lock = threading.Lock()
        
        # Статистика активности
        self.activity_stats = {
            "matches_started": 0,
            "matches_completed": 0,
            "queue_joins": 0,
            "queue_leaves": 0,
            "heartbeats_sent": 0,
            "errors": 0
        }
        
        # Поведенческие параметры (ЭКСТРЕМАЛЬНО АГРЕССИВНЫЕ для тестирования)
        self.behavior_settings = {
            BotBehavior.AGGRESSIVE: {
                "queue_probability": 0.98,  # ЭКСТРЕМАЛЬНАЯ активность
                "queue_duration": (300, 1800),  # Очень долгое ожидание в очереди
                "match_types": [MatchType.ONE_VS_ONE, MatchType.TWO_VS_TWO, MatchType.FOUR_PLAYER_FFA],
                "activity_interval": (1, 5)  # Максимально частые действия
            },
            BotBehavior.CASUAL: {
                "queue_probability": 0.95,  # Очень высокая активность
                "queue_duration": (240, 1200),  # Долгое ожидание
                "match_types": [MatchType.ONE_VS_ONE, MatchType.TWO_VS_TWO],
                "activity_interval": (2, 10)  # Очень частые действия
            },
            BotBehavior.PASSIVE: {
                "queue_probability": 0.85,  # Высокая активность (даже для пассивных!)
                "queue_duration": (600, 2400),  # Экстремально долгое ожидание
                "match_types": [MatchType.ONE_VS_ONE],
                "activity_interval": (5, 20)  # Частые действия
            },
            BotBehavior.RANDOM: {
                "queue_probability": 0.97,  # Почти 100% активность
                "queue_duration": (180, 900),  # Долгое ожидание
                "match_types": [MatchType.ONE_VS_ONE, MatchType.TWO_VS_TWO, MatchType.FOUR_PLAYER_FFA],
                "activity_interval": (1, 8)  # Максимально частые действия
            }
        }

    def load_bots_data(self) -> Dict:
        """📂 Загружает данные ботов из JSON файла"""
        try:
            if os.path.exists(self.bots_list_file):
                with open(self.bots_list_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    # Добавляем поведенческие параметры к существующим ботам
                    for bot_id, bot_data in data.items():
                        if "behavior" not in bot_data:
                            # Больше агрессивных ботов для активной игры
                            behaviors = ([BotBehavior.AGGRESSIVE] * 4 + 
                                        [BotBehavior.RANDOM] * 3 + 
                                        [BotBehavior.CASUAL] * 2 + 
                                        [BotBehavior.PASSIVE] * 1)
                            bot_data["behavior"] = random.choice(behaviors).value
                        if "in_queue" not in bot_data:
                            bot_data["in_queue"] = False
                        if "current_match_id" not in bot_data:
                            bot_data["current_match_id"] = None
                        if "queue_join_time" not in bot_data:
                            bot_data["queue_join_time"] = None
                        if "last_activity" not in bot_data:
                            bot_data["last_activity"] = None
                    logging.info(f"✅ Загружено {len(data)} ботов из файла")
                    return data
        except Exception as e:
            logging.error(f"❌ Ошибка загрузки данных ботов: {e}")
        return {}

    def save_bots_data(self):
        """💾 Сохраняет данные ботов в JSON файл"""
        try:
            with self.lock:
                with open(self.bots_list_file, 'w', encoding='utf-8') as f:
                    json.dump(self.bots_data, f, ensure_ascii=False, indent=2)
                logging.info(f"✅ Данные {len(self.bots_data)} ботов сохранены")
        except Exception as e:
            logging.error(f"❌ Ошибка сохранения данных ботов: {e}")

    def generate_unique_bot_name(self) -> str:
        """🎲 Генерирует уникальное имя для бота"""
        while True:
            name = random.choice(self.space_names)
            if random.random() < 0.7:  # 70% шанс добавить суффикс
                name += random.choice(self.space_suffixes)
            
            # Добавляем случайные цифры если нужно
            if random.random() < 0.3:  # 30% шанс добавить цифры
                name += str(random.randint(100, 999))
            
            # Проверяем уникальность
            if name not in [bot['username'] for bot in self.bots_data.values()]:
                return name

    def generate_bot_email(self, username: str) -> str:
        """📧 Генерирует email для бота"""
        domain = random.choice(['botmail.com', 'cybernet.ai', 'spacebotz.net', 'gameai.tech'])
        return f"{username.lower()}@{domain}"

    def generate_bot_password(self) -> str:
        """🔐 Генерирует безопасный пароль для бота"""
        return ''.join(random.choices(string.ascii_letters + string.digits, k=12))

    def register_bot(self, bot_id: str) -> Optional[Dict]:
        """🤖 Регистрирует одного бота через API"""
        try:
            username = self.generate_unique_bot_name()
            email = self.generate_bot_email(username)
            password = self.generate_bot_password()
            
            # Данные для регистрации
            register_data = {
                "username": username,
                "email": email,
                "password": password,
                "avatar": f"https://robohash.org/{username}?set=set1&size=200x200"
            }
            
            # Отправляем запрос регистрации
            response = self.session.post(
                self.register_endpoint,
                json=register_data,
                timeout=30
            )
            
            if response.status_code == 201:
                bot_data = response.json()
                
                # Сохраняем данные бота
                bot_info = {
                    "id": bot_data["id"],
                    "username": bot_data["username"],
                    "email": bot_data["email"],
                    "password": password,
                    "avatar": bot_data["avatar"],
                    "created_at": bot_data["createdAt"],
                    "games_played": bot_data.get("gamesPlayed", 0),
                    "games_won": bot_data.get("gamesWon", 0),
                    "score": bot_data.get("score", 0),
                    "level": bot_data.get("level", 1),
                    "last_login": None,
                    "last_heartbeat": None,
                    "status": "registered",
                    "behavior": random.choice([BotBehavior.AGGRESSIVE] * 4 + 
                                               [BotBehavior.RANDOM] * 3 + 
                                               [BotBehavior.CASUAL] * 2 + 
                                               [BotBehavior.PASSIVE] * 1).value,
                    "in_queue": False,
                    "current_match_id": None,
                    "queue_join_time": None,
                    "last_activity": None
                }
                
                with self.lock:
                    self.bots_data[bot_id] = bot_info
                
                logging.info(f"✅ Бот {username} (ID: {bot_data['id']}) успешно зарегистрирован")
                return bot_info
                
            else:
                logging.error(f"❌ Ошибка регистрации бота: {response.status_code} - {response.text}")
                return None
                
        except Exception as e:
            logging.error(f"❌ Исключение при регистрации бота {bot_id}: {e}")
            return None

    def login_bot(self, bot_id: str, bot_data: Dict) -> bool:
        """🔐 Авторизует бота в системе"""
        try:
            login_data = {
                "email": bot_data["email"],
                "password": bot_data["password"]
            }
            
            response = self.session.post(
                self.login_endpoint,
                json=login_data,
                timeout=30
            )
            
            if response.status_code == 200:
                user_data = response.json()
                
                # Обновляем данные бота после авторизации
                with self.lock:
                    self.bots_data[bot_id].update({
                        "last_login": datetime.now().isoformat(),
                        "status": "online",
                        "games_played": user_data.get("gamesPlayed", 0),
                        "games_won": user_data.get("gamesWon", 0),
                        "score": user_data.get("score", 0),
                        "level": user_data.get("level", 1)
                    })
                
                logging.info(f"✅ Бот {bot_data['username']} успешно авторизован")
                return True
            else:
                logging.error(f"❌ Ошибка авторизации бота {bot_data['username']}: {response.status_code}")
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при авторизации бота {bot_id}: {e}")
            return False

    def check_bot_status(self, bot_id: str, bot_data: Dict) -> bool:
        """💓 Проверяет статус бота (автоматически обновляет heartbeat на сервере)"""
        try:
            user_id = bot_data["id"]
            
            # Просто запрашиваем статус игрока - сервер автоматически обновит heartbeat
            response = self.session.get(
                f"{self.register_endpoint}/{user_id}",
                timeout=10
            )
            
            if response.status_code == 200:
                user_data = response.json()
                # Обновляем локальные данные бота
                with self.lock:
                    self.bots_data[bot_id]["last_heartbeat"] = datetime.now().isoformat()
                    self.bots_data[bot_id]["status"] = "online"
                    # Обновляем игровые данные если они изменились
                    self.bots_data[bot_id]["games_played"] = user_data.get("gamesPlayed", 0)
                    self.bots_data[bot_id]["games_won"] = user_data.get("gamesWon", 0)
                    self.bots_data[bot_id]["score"] = user_data.get("score", 0)
                    self.bots_data[bot_id]["level"] = user_data.get("level", 1)
                return True
            else:
                logging.warning(f"⚠️ Ошибка проверки статуса бота {bot_data['username']}: {response.status_code}")
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при проверке статуса бота {bot_id}: {e}")
            return False

    def check_all_bots_status(self) -> int:
        """💓 Проверяет статус всех ботов (автоматически обновляет heartbeat на сервере)"""
        success_count = 0
        
        with ThreadPoolExecutor(max_workers=20) as executor:
            futures = []
            
            for bot_id, bot_data in self.bots_data.items():
                future = executor.submit(self.check_bot_status, bot_id, bot_data)
                futures.append((bot_id, future))
            
            for bot_id, future in futures:
                try:
                    if future.result(timeout=15):
                        success_count += 1
                        self.activity_stats["heartbeats_sent"] += 1
                except Exception as e:
                    logging.error(f"❌ Ошибка проверки статуса бота {bot_id}: {e}")
                    self.activity_stats["errors"] += 1
        
        return success_count

    def join_queue(self, bot_id: str, bot_data: Dict, match_type: MatchType) -> bool:
        """🎯 Присоединяет бота к очереди"""
        try:
            user_id = bot_data["id"]
            bot_name = bot_data.get("username", "Unknown")
            
            # Проверяем, не в очереди ли уже бот
            if bot_data.get("in_queue", False):
                logging.warning(f"⚠️ Бот {bot_name} уже в очереди")
                return False
            
            # Проверяем, не в матче ли бот
            if bot_data.get("current_match_id"):
                logging.warning(f"⚠️ Бот {bot_name} уже в матче")
                return False
            
            queue_data = {
                "MatchType": match_type.value
            }
            
            logging.info(f"🎯 Отправляем запрос на вход в очередь для бота {bot_name} (ID: {user_id}), тип: {match_type.name} ({match_type.value})")
            
            response = self.session.post(
                f"{self.queue_endpoint}/{user_id}/join",
                json=queue_data,
                timeout=10
            )
            
            logging.info(f"📡 Ответ сервера для бота {bot_name}: {response.status_code}")
            
            if response.status_code == 200:
                with self.lock:
                    self.bots_data[bot_id]["in_queue"] = True
                    self.bots_data[bot_id]["queue_join_time"] = datetime.now().isoformat()
                    self.bots_data[bot_id]["last_activity"] = datetime.now().isoformat()
                    self.bots_data[bot_id]["current_match_type"] = match_type.value
                
                self.activity_stats["queue_joins"] += 1
                logging.info(f"✅ Бот {bot_name} успешно присоединился к очереди {match_type.name}")
                return True
            else:
                logging.error(f"❌ ОШИБКА входа в очередь для бота {bot_name}: {response.status_code} - {response.text}")
                # НЕ обновляем локальное состояние при ошибке
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при входе в очередь для бота {bot_id}: {e}")
            return False

    def leave_queue(self, bot_id: str, bot_data: Dict) -> bool:
        """🚪 Убирает бота из очереди"""
        try:
            user_id = bot_data["id"]
            bot_name = bot_data.get("username", "Unknown")
            
            # Проверяем, в очереди ли бот локально
            if not bot_data.get("in_queue", False):
                logging.warning(f"⚠️ Бот {bot_name} локально не в очереди")
                return False
            
            response = self.session.post(
                f"{self.queue_endpoint}/{user_id}/leave",
                timeout=10
            )
            
            if response.status_code == 200:
                with self.lock:
                    self.bots_data[bot_id]["in_queue"] = False
                    self.bots_data[bot_id]["queue_join_time"] = None
                    self.bots_data[bot_id]["last_activity"] = datetime.now().isoformat()
                    self.bots_data[bot_id]["current_match_type"] = None
                
                self.activity_stats["queue_leaves"] += 1
                logging.info(f"✅ Бот {bot_name} покинул очередь")
                return True
            elif response.status_code == 400 and "not in queue" in response.text:
                # Сервер говорит, что бот не в очереди - синхронизируем локальное состояние
                logging.warning(f"🔄 Синхронизация: Бот {bot_name} не в очереди на сервере, исправляем локальное состояние")
                with self.lock:
                    self.bots_data[bot_id]["in_queue"] = False
                    self.bots_data[bot_id]["queue_join_time"] = None
                    self.bots_data[bot_id]["current_match_type"] = None
                return False
            else:
                logging.error(f"❌ Ошибка выхода из очереди для бота {bot_name}: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при выходе из очереди для бота {bot_id}: {e}")
            return False

    def sync_queue_status(self, bot_id: str, bot_data: Dict) -> bool:
        """🔄 Синхронизирует статус очереди бота"""
        try:
            user_id = bot_data["id"]
            
            # Получаем статус очереди для бота
            response = self.session.get(
                f"{self.base_url}/api-game-queue/{user_id}/status",
                timeout=10
            )
            
            if response.status_code == 200:
                queue_data = response.json()
                
                with self.lock:
                    self.bots_data[bot_id]["queue_status"] = queue_data
                    
                    # Если матч найден, обновляем статус
                    if queue_data.get("matchFound", False):
                        self.bots_data[bot_id]["match_found"] = True
                        self.bots_data[bot_id]["match_id"] = queue_data.get("matchId")
                        self.bots_data[bot_id]["match_start_time"] = datetime.now().isoformat()
                        logging.info(f"🎯 Бот {bot_data['username']} найден матч {queue_data.get('matchId')}")
                    
                    # Если не в очереди, сбрасываем флаг
                    if not queue_data.get("inQueue", False):
                        self.bots_data[bot_id]["in_queue"] = False
                        
                return True
            else:
                logging.warning(f"⚠️ Ошибка синхронизации очереди для бота {bot_data['username']}: {response.status_code}")
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при синхронизации очереди бота {bot_id}: {e}")
            return False

    def check_and_finish_match(self, bot_id: str, bot_data: Dict) -> bool:
        """🏁 Проверяет и завершает матч бота если он длится слишком долго"""
        try:
            user_id = bot_data["id"]
            
            # Проверяем, есть ли активный матч
            response = self.session.get(
                f"{self.base_url}/api-game-match/user/{user_id}",
                timeout=10
            )
            
            if response.status_code == 200:
                matches_data = response.json()
                
                if matches_data and len(matches_data) > 0:
                    match = matches_data[0]  # Первый активный матч
                    match_id = match.get("matchId")
                    
                    if match_id and match.get("status") == "InProgress":
                        # Проверяем, когда матч начался
                        match_start_time = bot_data.get("match_start_time")
                        if match_start_time:
                            start_time = datetime.fromisoformat(match_start_time)
                            now = datetime.now()
                            match_duration = (now - start_time).total_seconds()
                            
                            # Если матч идет больше 30-90 секунд, завершаем его
                            if match_duration > random.randint(30, 90):
                                return self.finish_match(bot_id, bot_data, match_id, match)
                        else:
                            # Если нет времени начала, устанавливаем его
                            with self.lock:
                                self.bots_data[bot_id]["match_start_time"] = datetime.now().isoformat()
                                
                return True
            else:
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при проверке матча бота {bot_id}: {e}")
            return False

    def finish_match(self, bot_id: str, bot_data: Dict, match_id: int, match_data: Dict) -> bool:
        """🏁 Завершает матч бота со случайным результатом"""
        try:
            players_list = match_data.get("playersId", "[]")
            if isinstance(players_list, str):
                import json
                players_list = json.loads(players_list)
            
            if not players_list:
                logging.warning(f"⚠️ Нет игроков в матче {match_id}")
                return False
                
            # Выбираем случайного победителя
            winner_id = random.choice(players_list)
            losers = [p for p in players_list if p != winner_id]
            
            finish_data = {
                "Winners": [winner_id],
                "Losers": losers
            }
            
            response = self.session.post(
                f"{self.base_url}/api-game-match/{match_id}/finish",
                json=finish_data,
                timeout=10
            )
            
            if response.status_code == 200:
                logging.info(f"🏆 Бот {bot_data['username']} завершил матч {match_id}. Победитель: {winner_id}")
                
                # Очищаем данные о матче
                with self.lock:
                    self.bots_data[bot_id]["match_found"] = False
                    self.bots_data[bot_id]["match_id"] = None
                    self.bots_data[bot_id]["match_start_time"] = None
                    
                return True
            else:
                logging.error(f"❌ Ошибка завершения матча {match_id} для бота {bot_data['username']}: {response.status_code}")
                return False
                
        except Exception as e:
            logging.error(f"❌ Исключение при завершении матча бота {bot_id}: {e}")
            return False

    def simulate_bot_behavior(self, bot_id: str, bot_data: Dict) -> bool:
        """🎭 Симулирует поведение бота"""
        try:
            behavior = BotBehavior(bot_data.get("behavior", "random"))
            settings = self.behavior_settings[behavior]
            bot_name = bot_data.get("username", "Unknown")
            
            # Синхронизируем состояние очереди каждые 10 циклов
            if random.random() < 0.1:  # 10% шанс синхронизации
                self.sync_queue_status(bot_id, bot_data)
            
            # Проверяем, не в матче ли бот
            if bot_data.get("current_match_id"):
                return False
            
            # Если бот в очереди, проверяем timeout
            if bot_data.get("in_queue", False):
                queue_join_time = bot_data.get("queue_join_time")
                if queue_join_time:
                    join_time = datetime.fromisoformat(queue_join_time)
                    queue_duration = settings["queue_duration"]
                    max_wait = random.randint(*queue_duration)
                    
                    # Очень маленькая вероятность покинуть очередь (только 3% шанс)
                    if ((datetime.now() - join_time).total_seconds() > max_wait and 
                        random.random() < 0.03):
                        logging.info(f"🚪 Бот {bot_name} покидает очередь (timeout после {max_wait} сек)")
                        return self.leave_queue(bot_id, bot_data)
                
                # Даже если в очереди, возвращаем True как действие
                return True
            
            # Решаем, войти ли в очередь - БОЛЕЕ АГРЕССИВНО
            queue_chance = random.random()
            if queue_chance < settings["queue_probability"]:
                match_type = random.choice(settings["match_types"])
                logging.info(f"🎯 Бот {bot_name} ({behavior.value}) пытается войти в очередь {match_type.name} (chance: {queue_chance:.2f})")
                return self.join_queue(bot_id, bot_data, match_type)
            
            # Даже если не входим в очередь, возвращаем True как активность
            return True
            
        except Exception as e:
            logging.error(f"❌ Исключение при симуляции поведения бота {bot_id}: {e}")
            return False

    def create_bots_batch(self, start_index: int, count: int) -> List[Dict]:
        """🏭 Создает группу ботов параллельно"""
        results = []
        
        with ThreadPoolExecutor(max_workers=10) as executor:
            futures = []
            
            for i in range(start_index, start_index + count):
                bot_id = f"bot_{i:03d}"
                future = executor.submit(self.register_bot, bot_id)
                futures.append((bot_id, future))
            
            for bot_id, future in futures:
                try:
                    result = future.result(timeout=60)
                    if result:
                        results.append(result)
                except Exception as e:
                    logging.error(f"❌ Ошибка создания бота {bot_id}: {e}")
        
        return results

    def login_all_bots(self) -> int:
        """🔑 Авторизует всех ботов параллельно"""
        success_count = 0
        
        with ThreadPoolExecutor(max_workers=15) as executor:
            futures = []
            
            for bot_id, bot_data in self.bots_data.items():
                if bot_data.get("status") != "online":
                    future = executor.submit(self.login_bot, bot_id, bot_data)
                    futures.append((bot_id, future))
            
            for bot_id, future in futures:
                try:
                    if future.result(timeout=60):
                        success_count += 1
                except Exception as e:
                    logging.error(f"❌ Ошибка авторизации бота {bot_id}: {e}")
        
        return success_count

    def run_bot_activity_cycle(self) -> Dict:
        """🎮 Выполняет один цикл активности ботов"""
        logging.info("🔄 Запуск цикла активности ботов...")
        
        # Проверяем статус всех ботов (автоматически обновляет heartbeat на сервере)
        heartbeat_success = self.check_all_bots_status()
        
        # Подсчитываем онлайн ботов
        online_bots = [bot_id for bot_id, bot_data in self.bots_data.items() if bot_data.get("status") == "online"]
        logging.info(f"🤖 Онлайн ботов: {len(online_bots)}")
        
        # Симулируем поведение ботов
        with ThreadPoolExecutor(max_workers=30) as executor:
            futures = []
            
            for bot_id in online_bots:
                bot_data = self.bots_data[bot_id]
                future = executor.submit(self.simulate_bot_behavior, bot_id, bot_data)
                futures.append((bot_id, future))
            
            behavior_results = []
            for bot_id, future in futures:
                try:
                    result = future.result(timeout=30)
                    behavior_results.append(result)
                except Exception as e:
                    logging.error(f"❌ Ошибка симуляции поведения бота {bot_id}: {e}")
        
        # Проверяем и завершаем матчи ботов
        with ThreadPoolExecutor(max_workers=20) as executor:
            match_futures = []
            
            for bot_id in online_bots:
                bot_data = self.bots_data[bot_id]
                future = executor.submit(self.check_and_finish_match, bot_id, bot_data)
                match_futures.append((bot_id, future))
            
            match_results = []
            for bot_id, future in match_futures:
                try:
                    result = future.result(timeout=15)
                    match_results.append(result)
                except Exception as e:
                    logging.error(f"❌ Ошибка проверки матча бота {bot_id}: {e}")
        
        actions_count = sum(1 for r in behavior_results if r)
        matches_processed = sum(1 for r in match_results if r)
        in_queue_count = sum(1 for bot in self.bots_data.values() if bot.get("in_queue", False))
        
        logging.info(f"🎯 Действий выполнено: {actions_count}, Матчей обработано: {matches_processed}, В очереди: {in_queue_count}")
        
        # Сохраняем данные
        self.save_bots_data()
        
        return {
            "heartbeat_success": heartbeat_success,
            "behavior_actions": actions_count,
            "matches_processed": matches_processed,
            "total_bots": len(self.bots_data),
            "online_bots": len(online_bots),
            "in_queue": in_queue_count
        }

    def get_queue_statistics(self) -> Optional[Dict]:
        """📊 Получает статистику очередей"""
        try:
            response = self.session.get(f"{self.queue_endpoint}/stats", timeout=10)
            if response.status_code == 200:
                return response.json()
            else:
                logging.warning(f"⚠️ Ошибка получения статистики очередей: {response.status_code}")
                return None
        except Exception as e:
            logging.error(f"❌ Исключение при получении статистики очередей: {e}")
            return None

    def get_bots_statistics(self) -> Dict:
        """📊 Возвращает статистику по ботам"""
        total_bots = len(self.bots_data)
        online_bots = sum(1 for bot in self.bots_data.values() if bot.get("status") == "online")
        in_queue_bots = sum(1 for bot in self.bots_data.values() if bot.get("in_queue", False))
        total_games = sum(bot.get("games_played", 0) for bot in self.bots_data.values())
        total_wins = sum(bot.get("games_won", 0) for bot in self.bots_data.values())
        total_score = sum(bot.get("score", 0) for bot in self.bots_data.values())
        
        behavior_stats = {}
        for behavior in BotBehavior:
            behavior_stats[behavior.value] = sum(
                1 for bot in self.bots_data.values() 
                if bot.get("behavior") == behavior.value
            )
        
        return {
            "total_bots": total_bots,
            "online_bots": online_bots,
            "offline_bots": total_bots - online_bots,
            "in_queue_bots": in_queue_bots,
            "total_games": total_games,
            "total_wins": total_wins,
            "total_score": total_score,
            "average_score": total_score / total_bots if total_bots > 0 else 0,
            "behavior_distribution": behavior_stats,
            "activity_stats": self.activity_stats
        }

    def display_statistics(self, cycle_results: Dict = None, queue_stats: Dict = None):
        """📊 Отображает статистику"""
        stats = self.get_bots_statistics()
        
        print(f"""
        ╔══════════════════════════════════════════════════════════════╗
        ║                    📊 СТАТИСТИКА БОТОВ                       ║
        ╠══════════════════════════════════════════════════════════════╣
        ║ Всего ботов: {stats['total_bots']:>48} ║
        ║ Онлайн: {stats['online_bots']:>53} ║
        ║ В очереди: {stats['in_queue_bots']:>50} ║
        ║ Общий счет: {stats['total_score']:>49} ║
        ║ Всего игр: {stats['total_games']:>50} ║
        ║ Всего побед: {stats['total_wins']:>48} ║
        ║ Heartbeat отправлено: {stats['activity_stats']['heartbeats_sent']:>39} ║
        ║ Входов в очередь: {stats['activity_stats']['queue_joins']:>43} ║
        ║ Выходов из очереди: {stats['activity_stats']['queue_leaves']:>41} ║
        ╚══════════════════════════════════════════════════════════════╝
        """)
        
        if cycle_results:
            print(f"🔄 Последний цикл: {cycle_results['behavior_actions']} действий")
        
        if queue_stats:
            print(f"📊 Очереди: 1v1={queue_stats.get('oneVsOne', 0)}, 2v2={queue_stats.get('twoVsTwo', 0)}, FFA={queue_stats.get('fourPlayerFFA', 0)}")

    def run_continuous_activity(self, duration_minutes: int = None):
        """🔄 Запускает непрерывную активность ботов (бесконечно или с ограничением по времени)"""
        if duration_minutes is None:
            logging.info("🎮 Запуск БЕСКОНЕЧНОЙ активности ботов (остановка только через Ctrl+C)...")
            infinite_mode = True
        else:
            logging.info(f"🎮 Запуск непрерывной активности ботов на {duration_minutes} минут...")
            start_time = datetime.now()
            end_time = start_time + timedelta(minutes=duration_minutes)
            infinite_mode = False
        
        cycle_count = 0
        
        while True:
            try:
                # Проверяем условие завершения только если не в бесконечном режиме
                if not infinite_mode and datetime.now() >= end_time:
                    break
                
                cycle_count += 1
                logging.info(f"🔄 Цикл #{cycle_count}")
                
                # Выполняем цикл активности
                cycle_results = self.run_bot_activity_cycle()
                
                # Получаем статистику очередей
                queue_stats = self.get_queue_statistics()
                
                # Отображаем статистику каждые 5 циклов
                if cycle_count % 5 == 0:
                    self.display_statistics(cycle_results, queue_stats)
                
                # Показываем краткую информацию о работе каждые 50 циклов
                if cycle_count % 50 == 0:
                    logging.info(f"🚀 Менеджер ботов работает стабильно! Выполнено {cycle_count} циклов")
                
                # Пауза между циклами (МИНИМАЛЬНАЯ для максимальной активности)
                time.sleep(4)
                
            except KeyboardInterrupt:
                logging.info("⚠️ Остановка по запросу пользователя")
                break
            except Exception as e:
                logging.error(f"❌ Ошибка в цикле активности: {e}")
                time.sleep(10)
        
        logging.info(f"✅ Активность ботов завершена. Выполнено {cycle_count} циклов")

    def reset_queue_states(self):
        """🔄 Сбрасывает состояние очереди для всех ботов"""
        logging.info("🔄 Сброс состояния очереди для всех ботов...")
        
        reset_count = 0
        with self.lock:
            for bot_id, bot_data in self.bots_data.items():
                if bot_data.get("in_queue", False):
                    bot_data["in_queue"] = False
                    bot_data["queue_join_time"] = None
                    bot_data["current_match_type"] = None
                    reset_count += 1
        
        logging.info(f"✅ Сброшено состояние очереди для {reset_count} ботов")
        self.save_bots_data()

    def initialize_bots(self, target_count: int = 100):
        """🚀 Инициализирует ботов"""
        logging.info("🎮 Инициализация ботов...")
        
        # Сбрасываем состояние очереди при запуске
        self.reset_queue_states()
        
        # Проверяем, нужно ли создавать новых ботов
        existing_bots = len(self.bots_data)
        bots_to_create = target_count - existing_bots
        
        if bots_to_create > 0:
            logging.info(f"🤖 Создание {bots_to_create} новых ботов...")
            
            # Создаем ботов группами по 20
            batch_size = 20
            for i in range(0, bots_to_create, batch_size):
                current_batch = min(batch_size, bots_to_create - i)
                logging.info(f"📦 Создание группы ботов {i+1}-{i+current_batch}...")
                
                self.create_bots_batch(existing_bots + i + 1, current_batch)
                
                # Сохраняем данные после каждой группы
                self.save_bots_data()
                
                # Небольшая пауза между группами
                time.sleep(2)
        
        # Авторизуем всех ботов
        logging.info("🔐 Авторизация всех ботов...")
        success_count = self.login_all_bots()
        
        # Сохраняем обновленные данные
        self.save_bots_data()
        
        logging.info(f"✅ Инициализация завершена. Авторизовано {success_count} ботов")

    def sync_all_queue_states(self):
        """🔄 Принудительная синхронизация всех ботов с сервером"""
        logging.info("🔄 Принудительная синхронизация состояний очередей...")
        
        sync_count = 0
        with ThreadPoolExecutor(max_workers=20) as executor:
            futures = []
            
            for bot_id, bot_data in self.bots_data.items():
                if bot_data.get("status") == "online":
                    future = executor.submit(self.sync_queue_status, bot_id, bot_data)
                    futures.append((bot_id, future))
            
            for bot_id, future in futures:
                try:
                    if future.result(timeout=10):
                        sync_count += 1
                except Exception as e:
                    logging.error(f"❌ Ошибка синхронизации для бота {bot_id}: {e}")
        
        logging.info(f"✅ Синхронизировано {sync_count} ботов")

    def run_manager(self):
        """🚀 Запускает менеджер ботов"""
        logging.info("🎮 Запуск SECS Unified Bot Manager...")
        
        # Инициализируем ботов
        self.initialize_bots()
        
        # Проверяем статус всех ботов
        logging.info("💓 Проверка статуса всех ботов...")
        heartbeat_success = self.check_all_bots_status()
        
        # Принудительная синхронизация всех состояний
        self.sync_all_queue_states()
        
        # Сохраняем данные
        self.save_bots_data()
        
        # Выводим статистику
        self.display_statistics()
        
        # Запускаем БЕСКОНЕЧНУЮ активность (остановка только через Ctrl+C)
        logging.info("🚀 Переход в режим непрерывной работы...")
        self.run_continuous_activity()  # Без параметра = бесконечный режим
        
        logging.info("✅ Менеджер ботов завершил работу!")


def main():
    """🎯 Главная функция"""
    print("""
    ╔════════════════════════════════════════════════════════════════════════════════════════╗
    ║                       🎮 SECS Unified Bot Manager v2.1                                ║
    ║                     Space Epic Combat Simulator                                        ║
    ╠════════════════════════════════════════════════════════════════════════════════════════╣
    ║ Единый менеджер ботов с полным функционалом:                                          ║
    ║   • Создание и регистрация ботов                                                      ║
    ║   • Авторизация и heartbeat система                                                    ║
    ║   • Матчмейкинг и очереди                                                              ║
    ║   • Различные типы поведения ботов                                                     ║
    ║   • Подробная статистика и мониторинг                                                  ║
    ║                                                                                        ║
    ║ 🔄 БЕСКОНЕЧНЫЙ РЕЖИМ РАБОТЫ:                                                          ║
    ║   • Работает 24/7 пока не будет остановлен вручную                                   ║
    ║   • Остановка только через Ctrl+C                                                     ║
    ║   • Автоматическое восстановление после ошибок                                        ║
    ║                                                                                        ║
    ║ 🚀 Готов к космическим сражениям!                                                      ║
    ╚════════════════════════════════════════════════════════════════════════════════════════╝
    """)
    
    # Настройка логирования
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler('bot_manager.log', encoding='utf-8'),
            logging.StreamHandler()
        ]
    )
    
    try:
        manager = UnifiedBotsManager()
        manager.run_manager()
    except KeyboardInterrupt:
        print("\n⚠️ Остановка менеджера ботов...")
        logging.info("⚠️ Менеджер ботов остановлен пользователем")
    except Exception as e:
        print(f"\n❌ Критическая ошибка: {e}")
        logging.error(f"❌ Критическая ошибка: {e}")


if __name__ == "__main__":
    main() 