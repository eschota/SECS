# 
# создать endpoint для работы с очередями
# class queue_ticket: # билет в очередь для создания матча

#     queue_ticket_id: int - уникальный идентификатор очереди, auto increment
#     queue_match_type: int - очереди для разных типов матчей
#     queue_player: int - игрок
#     queue_ticket_register_time: datetime - время добавления в очередь
#     queue_ticket_match_type: int - тип матча
#     queue_ticket_player_mmr: int - рейтинг игрока для подбора матча.
#     queue_ticket_player_mmr_threshold: int - порог рейтинга игрока для подбора матча. изменяется соответственно all_mmr_min_limit_threshold и all_mmr_time_in_seconds_to_raise_threshold
 

# 
# Приоритет созданяи матча:
#  должны участвовать игроки с ближайшим рейтингом mmr к рейтингу игрока, который отправил запрос на вход в очередь.

# START:
# функция Create_Queues - создает все типы очередей с нуля при старте сервера.
# UPDATE every 1 second:
# функция ProcessQueue - проверяет очередь на матчи, сортирует игроков по времени, если есть условия удовлетворяющие созданию матча, то создается матч.
# on Demand:
# функция CreateMatch - создает матч, при удовлетворении условиям создания матча. Удаляет билеты из очереди и назначет player.current_match=match_id. для всех игроков в матче.

# Принцип работы очереди: бесконечно зацикленная функция ProcessQueue, которая проверяет очередь на матчи, сортирует игроков по времени создания билета в очередь , если есть условия удовлетворяющие созданию матча, то создается матч.
# Приоритет у игрока с большим временем ожидания в очереди.
# 
#
#
#
#
#
#
#
# Все запросы на сервер должны быть авторизованы токеном администратора.
#
#
#
#
#
# Queue Ограничения: 
# 1. игрок не может состоять в очереди на несколько типов матчей одновременно.
# 2. игрок не может попасть в очередь если он уже в матче ?player.current_match!=null.
# 
# Queue endpoints:
#/ GET - возвращает все очереди.
#/ POST - добавляет пользователя в очередь. с обязательными параметрами: queue_match_type, player_id.
#/ DELETE - удаляет пользователя из очереди. с обязательными параметрами:  player_id. перебор по всем очередям по player_id.
#/ PUT - обновляет данные очереди. с обязательными параметрами: queue_match_type, player_id.
# РАБОТА С ОЧЕРЕДЯМИ В ПАМЯТИ, БЕЗ ИСПОЛЬЗОВАНИЯ БАЗЫ ДАННЫХ. База данных используется только для работы с игроками.
# 
from flask import Blueprint, request, jsonify
from static import verify_admin_token
import json
import time
import threading
from datetime import datetime, timedelta
from dataclasses import dataclass
from typing import Dict, List, Optional
from database import db
import uuid

queue_bp = Blueprint('queue', __name__)

# Queue условия для создания матча:
# 1. разница в рейтинге между игроками должна быть не меньше mmr_min_limit_threshold
# 2. шаг повышения порога в процентах при превышении mmr_time_in_seconds_to_raise_threshold в ожидании очереди на матч

all_mmr_min_limit_threshold = 25 # минимальная разница для создания матча
all_mmr_time_in_seconds_to_raise_threshold = 10 # время в секундах для повышения порога
all_mmr_raise_threshold_step = 0.1 # шаг повышения порога в процентах при превышенииmmr_time_in_seconds_to_raise_threshold в ожидании очереди на матч

# Типы матчей и их требования
MATCH_TYPES = {
    0: {'name': '1v1', 'players_required': 2},
    1: {'name': '2v2', 'players_required': 4},
    2: {'name': '3v3', 'players_required': 6},
    3: {'name': '4v4', 'players_required': 8},
    4: {'name': '5v5', 'players_required': 10},
    5: {'name': '6v6', 'players_required': 12}
}

@dataclass
class QueueTicket:
    """Билет в очередь для создания матча"""
    queue_ticket_id: str
    queue_match_type: int
    queue_player: str
    queue_ticket_register_time: datetime
    queue_ticket_player_mmr: int
    queue_ticket_player_mmr_threshold: int
    username: str = ""
    
    def __post_init__(self):
        if isinstance(self.queue_ticket_register_time, str):
            self.queue_ticket_register_time = datetime.fromisoformat(self.queue_ticket_register_time)

# Глобальное хранилище очередей в памяти
queues: Dict[int, List[QueueTicket]] = {}
queue_lock = threading.Lock()
queue_processor_running = False

def init_queues():
    """Инициализация очередей"""
    global queues
    for match_type in MATCH_TYPES.keys():
        queues[match_type] = []

def get_player_mmr(player_id: str, match_type: int) -> int:
    """Получает MMR игрока для определенного типа матча"""
    user = db.get_user(player_id)
    if not user:
        return 1000  # Начальный рейтинг по умолчанию
    
    try:
        mmr_list = json.loads(user.get('mmr', '[1000, 1000, 1000, 1000, 1000, 1000]'))
        return mmr_list[match_type] if match_type < len(mmr_list) else 1000
    except:
        return 1000

def calculate_mmr_threshold(ticket: QueueTicket) -> int:
    """Рассчитывает текущий порог MMR для билета"""
    time_in_queue = (datetime.now() - ticket.queue_ticket_register_time).total_seconds()
    
    if time_in_queue <= all_mmr_time_in_seconds_to_raise_threshold:
        return all_mmr_min_limit_threshold
    
    # Увеличиваем порог со временем
    time_multiplier = int(time_in_queue / all_mmr_time_in_seconds_to_raise_threshold)
    threshold = all_mmr_min_limit_threshold * (1 + all_mmr_raise_threshold_step * time_multiplier)
    
    return int(threshold)

def can_create_match(tickets: List[QueueTicket], match_type: int) -> Optional[List[QueueTicket]]:
    """Проверяет, можно ли создать матч из билетов"""
    required_players = MATCH_TYPES[match_type]['players_required']
    
    if len(tickets) < required_players:
        return None
    
    # Сортируем билеты по времени создания (приоритет у тех, кто дольше ждет)
    tickets.sort(key=lambda x: x.queue_ticket_register_time)
    
    # Берем первый билет как основу для поиска
    base_ticket = tickets[0]
    base_mmr = base_ticket.queue_ticket_player_mmr
    base_threshold = calculate_mmr_threshold(base_ticket)
    
    # Находим подходящих игроков
    suitable_tickets = []
    for ticket in tickets:
        if len(suitable_tickets) >= required_players:
            break
        
        ticket_mmr = ticket.queue_ticket_player_mmr
        ticket_threshold = calculate_mmr_threshold(ticket)
        
        # Проверяем, подходит ли игрок по MMR
        mmr_diff = abs(base_mmr - ticket_mmr)
        if mmr_diff <= max(base_threshold, ticket_threshold):
            suitable_tickets.append(ticket)
    
    if len(suitable_tickets) >= required_players:
        return suitable_tickets[:required_players]
    
    return None

def create_match_from_tickets(tickets: List[QueueTicket], match_type: int) -> str:
    """Создает матч из билетов"""
    from match_endpoints import create_match_internal
    
    # Создаем матч
    match_data = {
        'match_type': MATCH_TYPES[match_type]['name'],
        'players': [ticket.queue_player for ticket in tickets],
        'status': 'starting'
    }
    
    match_id = create_match_internal(match_data)
    
    # Обновляем данные игроков
    for ticket in tickets:
        try:
            user = db.get_user(ticket.queue_player)
            if user:
                profile_data = json.loads(user.get('profile_data', '{}'))
                profile_data['match_current'] = match_id
                profile_data['queue_ticket_id'] = None
                
                db.update_user({
                    'user_id': ticket.queue_player,
                    'profile_data': json.dumps(profile_data)
                })
        except Exception as e:
            print(f"Error updating player {ticket.queue_player}: {e}")
    
    return match_id

def process_queue():
    """Обрабатывает очередь на создание матчей"""
    global queue_processor_running
    
    if queue_processor_running:
        return
    
    queue_processor_running = True
    
    try:
        with queue_lock:
            for match_type, tickets in queues.items():
                if not tickets:
                    continue
                
                # Пытаемся создать матч
                match_tickets = can_create_match(tickets, match_type)
                if match_tickets:
                    # Создаем матч
                    match_id = create_match_from_tickets(match_tickets, match_type)
                    
                    # Удаляем билеты из очереди
                    for ticket in match_tickets:
                        if ticket in tickets:
                            tickets.remove(ticket)
                    
                    print(f"Created match {match_id} for {len(match_tickets)} players")
    
    except Exception as e:
        print(f"Error in queue processor: {e}")
    finally:
        queue_processor_running = False

# Инициализируем очереди при загрузке модуля
init_queues()

# Запускаем обработку очередей каждую секунду
def start_queue_processor():
    """Запускает обработчик очередей"""
    def run_processor():
        while True:
            process_queue()
            time.sleep(1)
    
    processor_thread = threading.Thread(target=run_processor, daemon=True)
    processor_thread.start()

# Запускаем обработчик при загрузке модуля
start_queue_processor()

@queue_bp.route('/', methods=['GET'])
def get_queues():
    """Возвращает все очереди"""
    with queue_lock:
        result = {}
        for match_type, tickets in queues.items():
            result[match_type] = {
                'match_type': MATCH_TYPES[match_type]['name'],
                'players_required': MATCH_TYPES[match_type]['players_required'],
                'current_players': len(tickets),
                'tickets': [{
                    'queue_ticket_id': ticket.queue_ticket_id,
                    'player_id': ticket.queue_player,
                    'username': ticket.username,
                    'mmr': ticket.queue_ticket_player_mmr,
                    'wait_time': (datetime.now() - ticket.queue_ticket_register_time).total_seconds(),
                    'mmr_threshold': calculate_mmr_threshold(ticket)
                } for ticket in tickets]
            }
    
    return jsonify({
        "status": "success",
        "queues": result
    })

@queue_bp.route('/', methods=['POST'])
def add_to_queue():
    """Добавляет пользователя в очередь"""
    data = request.get_json()
    if not data or 'queue_match_type' not in data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "queue_match_type and player_id required"}), 400
    
    match_type = data['queue_match_type']
    player_id = data['player_id']
    
    # Проверяем валидность типа матча
    if match_type not in MATCH_TYPES:
        return jsonify({"status": "error", "message": "Invalid match type"}), 400
    
    # Проверяем, существует ли пользователь
    user = db.get_user(player_id)
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    # Проверяем, не находится ли игрок уже в матче
    profile_data = json.loads(user.get('profile_data', '{}'))
    if profile_data.get('match_current'):
        return jsonify({"status": "error", "message": "Player is already in a match"}), 409
    
    # Проверяем, не находится ли игрок уже в очереди
    with queue_lock:
        for queue_type, tickets in queues.items():
            for ticket in tickets:
                if ticket.queue_player == player_id:
                    return jsonify({"status": "error", "message": "Player is already in queue"}), 409
    
    # Создаем билет в очередь
    ticket = QueueTicket(
        queue_ticket_id=str(uuid.uuid4()),
        queue_match_type=match_type,
        queue_player=player_id,
        queue_ticket_register_time=datetime.now(),
        queue_ticket_player_mmr=get_player_mmr(player_id, match_type),
        queue_ticket_player_mmr_threshold=all_mmr_min_limit_threshold,
        username=user['nick_name']
    )
    
    # Добавляем в очередь
    with queue_lock:
        queues[match_type].append(ticket)
    
    # Обновляем данные игрока
    profile_data['queue_ticket_id'] = ticket.queue_ticket_id
    db.update_user({
        'user_id': player_id,
        'profile_data': json.dumps(profile_data)
    })
    
    return jsonify({
        "status": "success",
        "message": "Added to queue successfully",
        "ticket": {
            "queue_ticket_id": ticket.queue_ticket_id,
            "match_type": MATCH_TYPES[match_type]['name'],
            "position": len(queues[match_type]),
            "estimated_wait_time": "calculating..."
        }
    })

@queue_bp.route('/', methods=['DELETE'])
def remove_from_queue():
    """Удаляет пользователя из очереди"""
    data = request.get_json()
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id required"}), 400
    
    player_id = data['player_id']
    removed_ticket = None
    
    # Ищем и удаляем билет из всех очередей
    with queue_lock:
        for match_type, tickets in queues.items():
            for ticket in tickets[:]:  # Копируем список для безопасного удаления
                if ticket.queue_player == player_id:
                    tickets.remove(ticket)
                    removed_ticket = ticket
                    break
            if removed_ticket:
                break
    
    if not removed_ticket:
        return jsonify({"status": "error", "message": "Player not found in queue"}), 404
    
    # Обновляем данные игрока
    user = db.get_user(player_id)
    if user:
        profile_data = json.loads(user.get('profile_data', '{}'))
        profile_data['queue_ticket_id'] = None
        db.update_user({
            'user_id': player_id,
            'profile_data': json.dumps(profile_data)
        })
    
    return jsonify({
        "status": "success",
        "message": "Removed from queue successfully",
        "ticket": {
            "queue_ticket_id": removed_ticket.queue_ticket_id,
            "match_type": MATCH_TYPES[removed_ticket.queue_match_type]['name']
        }
    })

@queue_bp.route('/', methods=['PUT'])
def update_queue():
    """Обновляет данные очереди (только для админа)"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data:
        return jsonify({"status": "error", "message": "No data provided"}), 400
    
    # Можно обновить настройки матчмейкинга
    global all_mmr_min_limit_threshold, all_mmr_time_in_seconds_to_raise_threshold, all_mmr_raise_threshold_step
    
    if 'mmr_min_limit_threshold' in data:
        all_mmr_min_limit_threshold = data['mmr_min_limit_threshold']
    if 'mmr_time_in_seconds_to_raise_threshold' in data:
        all_mmr_time_in_seconds_to_raise_threshold = data['mmr_time_in_seconds_to_raise_threshold']
    if 'mmr_raise_threshold_step' in data:
        all_mmr_raise_threshold_step = data['mmr_raise_threshold_step']
    
    return jsonify({
        "status": "success",
        "message": "Queue settings updated",
        "settings": {
            "mmr_min_limit_threshold": all_mmr_min_limit_threshold,
            "mmr_time_in_seconds_to_raise_threshold": all_mmr_time_in_seconds_to_raise_threshold,
            "mmr_raise_threshold_step": all_mmr_raise_threshold_step
        }
    })

@queue_bp.route('/player/<player_id>', methods=['GET'])
def get_player_queue_status(player_id):
    """Получает статус игрока в очереди"""
    with queue_lock:
        for match_type, tickets in queues.items():
            for ticket in tickets:
                if ticket.queue_player == player_id:
                    wait_time = (datetime.now() - ticket.queue_ticket_register_time).total_seconds()
                    
                    return jsonify({
                        "inQueue": True,
                        "queueType": match_type,
                        "queueTime": int(wait_time),
                        "currentMmrThreshold": calculate_mmr_threshold(ticket),
                        "userMmr": ticket.queue_ticket_player_mmr,
                        "matchId": "",  # Empty unless match found
                        "matchFound": False  # Will be True when match is found
                    })
    
    return jsonify({
        "inQueue": False,
        "queueType": 0,
        "queueTime": 0,
        "currentMmrThreshold": 0,
        "userMmr": 0,
        "matchId": "",
        "matchFound": False
    })

@queue_bp.route('/clear', methods=['POST'])
def clear_queue():
    """Очищает все очереди (только для админа)"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    with queue_lock:
        for match_type in queues:
            queues[match_type] = []
    
    return jsonify({
        "status": "success",
        "message": "All queues cleared"
    })

