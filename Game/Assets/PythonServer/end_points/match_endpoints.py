# match_endpoints.py

#Техническое задание:
# создать endpoint для работы с матчами
# матч - это игра между двумя игроками или более игроками.
# class Match:
#     match_type: str - тип матча:
#         - 1v1
#         - 2v2
#         - 3v3
#         - 4v4
#         - 5v5
#         - 6v6
#     match_id: int - порядковый номер матча, уникальный идентификатор - auto increment
#     players: list[str] - список игроков в матче
#     start_time: datetime - время начала матча
#     end_time: datetime - время окончания матча
#     status: str
#     game_actions: list[dict]
#     result_win: list[user_id] - список игроков, которые выиграли матч
#     result_lose: list[user_id] - список игроков, которые проиграли матч
#     result_surrender: list[user_id] - список игроков, которые сдались
#     result_draw: list[user_id] - список игроков, которые сыграли вничью
#     result_cancel: list[user_id] - список игроков, которые отменили матч
# class GameAction:
#     action_id: int - порядковый номер действия, уникальный идентификатор - auto increment
#     player_id: int - id игрока
#     action_type: str - тип действия:

# Matches:
# matches_active: list[Match] - список активных матчей
# matches_history: list[Match] - список завершенных матчей

#







from flask import Blueprint, request, jsonify
from static import verify_admin_token
from database import db
from datetime import datetime, timedelta
from dataclasses import dataclass, field
from typing import List, Any, Optional, Dict
import uuid
import json
import threading

match_bp = Blueprint('match', __name__)

# Временные ограничения для матчей (в секундах)
match_type_time_limit = {
    '1v1': 300,   # 5 минут
    '2v2': 600,   # 10 минут
    '3v3': 900,   # 15 минут
    '4v4': 1200,  # 20 минут
    '5v5': 1500,  # 25 минут
    '6v6': 1800   # 30 минут
}

@dataclass
class GameAction:
    """Игровое действие"""
    action_id: str
    player_id: str
    action_type: str
    action_data: dict
    timestamp: datetime
    
    def __post_init__(self):
        if isinstance(self.timestamp, str):
            self.timestamp = datetime.fromisoformat(self.timestamp)

@dataclass
class Match:
    """Матч между игроками"""
    match_id: str
    match_type: str
    players: List[str]
    start_time: datetime
    end_time: Optional[datetime] = None
    status: str = "waiting"  # waiting, starting, active, finished, cancelled
    game_actions: List[GameAction] = field(default_factory=list)
    result_win: List[str] = field(default_factory=list)
    result_lose: List[str] = field(default_factory=list)
    result_surrender: List[str] = field(default_factory=list)
    result_draw: List[str] = field(default_factory=list)
    result_cancel: List[str] = field(default_factory=list)
    
    def __post_init__(self):
        if isinstance(self.start_time, str):
            self.start_time = datetime.fromisoformat(self.start_time)
        if isinstance(self.end_time, str):
            self.end_time = datetime.fromisoformat(self.end_time)

# Глобальные хранилища матчей
matches_active: Dict[str, Match] = {}
matches_history: Dict[str, Match] = {}
matches_lock = threading.Lock()

def create_match_internal(match_data: dict) -> str:
    """Внутренняя функция для создания матча (используется системой очередей)"""
    match_id = str(uuid.uuid4())
    
    match = Match(
        match_id=match_id,
        match_type=match_data['match_type'],
        players=match_data['players'],
        start_time=datetime.now(),
        status=match_data.get('status', 'starting')
    )
    
    # Сохраняем в активные матчи
    with matches_lock:
        matches_active[match_id] = match
    
    # Сохраняем в базу данных
    try:
        db.create_game({
            'match_id': match_id,
            'name': f"{match_data['match_type']} Match",
            'status': match.status,
            'max_players': len(match_data['players']),
            'current_players': len(match_data['players']),
            'players': json.dumps(match_data['players'])
        })
    except Exception as e:
        print(f"Error saving match to database: {e}")
    
    return match_id

def finish_match(match_id: str, result_data: dict):
    """Завершает матч с результатами"""
    with matches_lock:
        if match_id not in matches_active:
            return False
        
        match = matches_active[match_id]
        match.end_time = datetime.now()
        match.status = "finished"
        
        # Обновляем результаты
        match.result_win = result_data.get('winners', [])
        match.result_lose = result_data.get('losers', [])
        match.result_surrender = result_data.get('surrendered', [])
        match.result_draw = result_data.get('draw', [])
        
        # Переносим в историю
        matches_history[match_id] = match
        del matches_active[match_id]
        
        # Обновляем данные игроков
        for player_id in match.players:
            try:
                user = db.get_user(player_id)
                if user:
                    profile_data = json.loads(user.get('profile_data', '{}'))
                    profile_data['match_current'] = None
                    
                    db.update_user({
                        'user_id': player_id,
                        'profile_data': json.dumps(profile_data)
                    })
            except Exception as e:
                print(f"Error updating player {player_id}: {e}")
        
        # Обновляем в базе данных
        try:
            db.update_game({
                'match_id': match_id,
                'status': 'finished',
                'ended_at': match.end_time.isoformat()
            })
        except Exception as e:
            print(f"Error updating match in database: {e}")
        
        return True

def cancel_match(match_id: str, reason: str = "timeout"):
    """Отменяет матч"""
    with matches_lock:
        if match_id not in matches_active:
            return False
        
        match = matches_active[match_id]
        match.end_time = datetime.now()
        match.status = "cancelled"
        match.result_cancel = match.players.copy()
        
        # Переносим в историю
        matches_history[match_id] = match
        del matches_active[match_id]
        
        # Обновляем данные игроков
        for player_id in match.players:
            try:
                user = db.get_user(player_id)
                if user:
                    profile_data = json.loads(user.get('profile_data', '{}'))
                    profile_data['match_current'] = None
                    
                    db.update_user({
                        'user_id': player_id,
                        'profile_data': json.dumps(profile_data)
                    })
            except Exception as e:
                print(f"Error updating player {player_id}: {e}")
        
        # Обновляем в базе данных
        try:
            db.update_game({
                'match_id': match_id,
                'status': 'cancelled'
            })
        except Exception as e:
            print(f"Error updating match in database: {e}")
        
        return True

def check_match_timeouts():
    """Проверяет матчи на превышение времени"""
    current_time = datetime.now()
    
    with matches_lock:
        matches_to_cancel = []
        
        for match_id, match in matches_active.items():
            if match.match_type not in match_type_time_limit:
                continue
            
            time_limit = match_type_time_limit[match.match_type]
            if (current_time - match.start_time).total_seconds() > time_limit:
                matches_to_cancel.append(match_id)
        
        for match_id in matches_to_cancel:
            cancel_match(match_id, "timeout")

# Запускаем проверку таймаутов каждые 30 секунд
def start_timeout_checker():
    """Запускает проверку таймаутов матчей"""
    def run_checker():
        import time
        while True:
            check_match_timeouts()
            time.sleep(30)
    
    checker_thread = threading.Thread(target=run_checker, daemon=True)
    checker_thread.start()

# Запускаем проверку при загрузке модуля
start_timeout_checker()

@match_bp.route('/', methods=['GET'])
def get_matches():
    """Возвращает список всех матчей"""
    with matches_lock:
        active_matches = []
        for match_id, match in matches_active.items():
            active_matches.append({
                'match_id': match_id,
                'match_type': match.match_type,
                'players': match.players,
                'status': match.status,
                'start_time': match.start_time.isoformat(),
                'duration': (datetime.now() - match.start_time).total_seconds()
            })
        
        history_matches = []
        for match_id, match in matches_history.items():
            history_matches.append({
                'match_id': match_id,
                'match_type': match.match_type,
                'players': match.players,
                'status': match.status,
                'start_time': match.start_time.isoformat(),
                'end_time': match.end_time.isoformat() if match.end_time else None,
                'duration': (match.end_time - match.start_time).total_seconds() if match.end_time else None,
                'result': {
                    'winners': match.result_win,
                    'losers': match.result_lose,
                    'surrendered': match.result_surrender,
                    'draw': match.result_draw,
                    'cancelled': match.result_cancel
                }
            })
    
    return jsonify({
        "status": "success",
        "active_matches": active_matches,
        "history_matches": history_matches,
        "total_active": len(active_matches),
        "total_history": len(history_matches)
    })

@match_bp.route('/', methods=['POST'])
def create_match():
    """Создает новый матч"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'match_type' not in data or 'players' not in data:
        return jsonify({"status": "error", "message": "match_type and players required"}), 400
    
    # Проверяем валидность типа матча
    if data['match_type'] not in match_type_time_limit:
        return jsonify({"status": "error", "message": "Invalid match type"}), 400
    
    # Проверяем, что все игроки существуют
    for player_id in data['players']:
        user = db.get_user(player_id)
        if not user:
            return jsonify({"status": "error", "message": f"Player {player_id} not found"}), 404
    
    try:
        match_id = create_match_internal(data)
        
        return jsonify({
            "status": "success",
            "match_id": match_id,
            "message": "Match created successfully"
        }), 201
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 500

@match_bp.route('/', methods=['PUT'])
def update_match():
    """Обновляет данные матча"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'match_id' not in data:
        return jsonify({"status": "error", "message": "match_id required"}), 400
    
    match_id = data['match_id']
    
    with matches_lock:
        if match_id not in matches_active:
            return jsonify({"status": "error", "message": "Match not found or not active"}), 404
        
        match = matches_active[match_id]
        
        # Обновляем статус
        if 'status' in data:
            match.status = data['status']
        
        # Добавляем игровые действия
        if 'action' in data:
            action_data = data['action']
            action = GameAction(
                action_id=str(uuid.uuid4()),
                player_id=action_data['player_id'],
                action_type=action_data['action_type'],
                action_data=action_data.get('data', {}),
                timestamp=datetime.now()
            )
            match.game_actions.append(action)
    
    return jsonify({
        "status": "success",
        "message": "Match updated successfully"
    })

@match_bp.route('/', methods=['DELETE'])
def delete_match():
    """Удаляет/отменяет матч"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'match_id' not in data:
        return jsonify({"status": "error", "message": "match_id required"}), 400
    
    match_id = data['match_id']
    
    if cancel_match(match_id, "admin_cancelled"):
        return jsonify({
            "status": "success",
            "message": "Match cancelled successfully"
        })
    else:
        return jsonify({
            "status": "error",
            "message": "Match not found or not active"
        }), 404

@match_bp.route('/<match_id>', methods=['GET'])
def get_match(match_id):
    """Получает данные конкретного матча"""
    with matches_lock:
        # Ищем в активных матчах
        if match_id in matches_active:
            match = matches_active[match_id]
            return jsonify({
                "status": "success",
                "match": {
                    'match_id': match_id,
                    'match_type': match.match_type,
                    'players': match.players,
                    'status': match.status,
                    'start_time': match.start_time.isoformat(),
                    'duration': (datetime.now() - match.start_time).total_seconds(),
                    'actions': [{
                        'action_id': action.action_id,
                        'player_id': action.player_id,
                        'action_type': action.action_type,
                        'action_data': action.action_data,
                        'timestamp': action.timestamp.isoformat()
                    } for action in match.game_actions]
                }
            })
        
        # Ищем в истории
        if match_id in matches_history:
            match = matches_history[match_id]
            return jsonify({
                "status": "success",
                "match": {
                    'match_id': match_id,
                    'match_type': match.match_type,
                    'players': match.players,
                    'status': match.status,
                    'start_time': match.start_time.isoformat(),
                    'end_time': match.end_time.isoformat() if match.end_time else None,
                    'duration': (match.end_time - match.start_time).total_seconds() if match.end_time else None,
                    'result': {
                        'winners': match.result_win,
                        'losers': match.result_lose,
                        'surrendered': match.result_surrender,
                        'draw': match.result_draw,
                        'cancelled': match.result_cancel
                    },
                    'actions': [{
                        'action_id': action.action_id,
                        'player_id': action.player_id,
                        'action_type': action.action_type,
                        'action_data': action.action_data,
                        'timestamp': action.timestamp.isoformat()
                    } for action in match.game_actions]
                }
            })
    
    return jsonify({
        "status": "error",
        "message": "Match not found"
    }), 404

@match_bp.route('/<match_id>/finish', methods=['POST'])
def finish_match_endpoint(match_id):
    """Завершает матч с результатами"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data:
        return jsonify({"status": "error", "message": "Result data required"}), 400
    
    if finish_match(match_id, data):
        return jsonify({
            "status": "success",
            "message": "Match finished successfully"
        })
    else:
        return jsonify({
            "status": "error",
            "message": "Match not found or not active"
        }), 404

@match_bp.route('/<match_id>/surrender', methods=['POST'])
def surrender_match(match_id):
    """Игрок сдается в матче"""
    data = request.get_json()
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id required"}), 400
    
    player_id = data['player_id']
    
    with matches_lock:
        if match_id not in matches_active:
            return jsonify({"status": "error", "message": "Match not found or not active"}), 404
        
        match = matches_active[match_id]
        
        if player_id not in match.players:
            return jsonify({"status": "error", "message": "Player not in this match"}), 404
        
        # Добавляем игрока к сдавшимся
        if player_id not in match.result_surrender:
            match.result_surrender.append(player_id)
        
        # Добавляем действие
        action = GameAction(
            action_id=str(uuid.uuid4()),
            player_id=player_id,
            action_type="surrender",
            action_data={"reason": data.get("reason", "player_surrender")},
            timestamp=datetime.now()
        )
        match.game_actions.append(action)
    
    return jsonify({
        "status": "success",
        "message": "Player surrendered successfully"
    })

@match_bp.route('/<match_id>/action', methods=['POST'])
def add_game_action(match_id):
    """Добавляет игровое действие в матч"""
    data = request.get_json()
    if not data or 'player_id' not in data or 'action_type' not in data:
        return jsonify({"status": "error", "message": "player_id and action_type required"}), 400
    
    with matches_lock:
        if match_id not in matches_active:
            return jsonify({"status": "error", "message": "Match not found or not active"}), 404
        
        match = matches_active[match_id]
        
        if data['player_id'] not in match.players:
            return jsonify({"status": "error", "message": "Player not in this match"}), 404
        
        # Добавляем действие
        action = GameAction(
            action_id=str(uuid.uuid4()),
            player_id=data['player_id'],
            action_type=data['action_type'],
            action_data=data.get('action_data', {}),
            timestamp=datetime.now()
        )
        match.game_actions.append(action)
    
    return jsonify({
        "status": "success",
        "message": "Action added successfully",
        "action_id": action.action_id
    })

@match_bp.route('/player/<player_id>', methods=['GET'])
def get_player_matches(player_id):
    """Получает матчи игрока"""
    current_match = None
    player_history = []
    
    with matches_lock:
        # Ищем текущий матч
        for match_id, match in matches_active.items():
            if player_id in match.players:
                current_match = {
                    'match_id': match_id,
                    'match_type': match.match_type,
                    'players': match.players,
                    'status': match.status,
                    'start_time': match.start_time.isoformat(),
                    'duration': (datetime.now() - match.start_time).total_seconds()
                }
                break
        
        # Ищем в истории
        for match_id, match in matches_history.items():
            if player_id in match.players:
                player_history.append({
                    'match_id': match_id,
                    'match_type': match.match_type,
                    'players': match.players,
                    'status': match.status,
                    'start_time': match.start_time.isoformat(),
                    'end_time': match.end_time.isoformat() if match.end_time else None,
                    'duration': (match.end_time - match.start_time).total_seconds() if match.end_time else None,
                    'result': {
                        'won': player_id in match.result_win,
                        'lost': player_id in match.result_lose,
                        'surrendered': player_id in match.result_surrender,
                        'draw': player_id in match.result_draw,
                        'cancelled': player_id in match.result_cancel
                    }
                })
    
    return jsonify({
        "status": "success",
        "current_match": current_match,
        "match_history": player_history,
        "total_matches": len(player_history)
    })

@match_bp.route('/clear_history', methods=['POST'])
def clear_match_history():
    """Очищает историю матчей (только для админа)"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    with matches_lock:
        matches_history.clear()
    
    return jsonify({
        "status": "success",
        "message": "Match history cleared"
    })


