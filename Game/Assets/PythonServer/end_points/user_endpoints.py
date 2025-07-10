#Техническое задание:
# создать endpoint для работы с пользователями
# class User:
#     player_id: int - уникальный идентификатор пользователя, auto increment, отрицательные значения - боты.
#     nick_name: str - никнейм пользователя
#     email: str - email пользователя
#     password: str - пароль пользователя
#     avatar_url: str - url аватарки пользователя
#     mmr: [int] - массив рейтингов пользователя, по типам матча, на старте рейтинг равен 0. match_endpoints.py Match match_type: int
#     match_current: int - id текущего матча пользователя, нуллабл
#     queue_ticket_id: int - id текущего билета в очереди пользователя, нуллабл
#     money: float - деньги пользователя
#     last_login_time: datetime - время последнего входа в игру
#     last_logout_time: datetime - время последнего выхода из игры
#     last_login_ip: str - ip последнего входа в игру
#     last_logout_ip: str - ip последнего выхода из игры
#     last_login_device: str - устройство последнего входа в игру
#     last_logout_device: str - устройство последнего выхода из игры
#
#
#
#
#
#
#
#
# # Все запросы на сервер должны быть авторизованы токеном администратора.
#
#
#
#
#
#
from static import verify_admin_token # ВАЖНО - ЭТО ОПЦИИ И НАСТРОЙКИ ИГРЫ ДЛЯ РАБОТЫ С игроками, обязательно прочитай и секцию user с инструкциями.

from flask import Blueprint, request, jsonify
from database import db
from dataclasses import dataclass, asdict
import json

user_bp = Blueprint('user', __name__)



@dataclass
class User:
    player_id: str
    nick_name: str
    email: str
    password: str
    avatar_url: str = ""
    mmr: str = "[]"  # JSON массив рейтингов
    match_current: int = None  # ID текущего матча
    queue_ticket_id: int = None  # ID билета в очереди
    money: float = 0.0
    last_login_time: str = None
    last_logout_time: str = None
    last_login_ip: str = ""
    last_logout_ip: str = ""
    last_login_device: str = ""
    last_logout_device: str = ""
    status: str = "active"
    profile_data: str = "{}"

@user_bp.route('/', methods=['GET'])
def get_user():
    """Возвращает данные пользователя"""
    player_id = request.args.get('player_id')
    
    if not player_id:
        return jsonify({"status": "error", "message": "player_id parameter is required"}), 400
    
    user = db.get_user(player_id)
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    return jsonify({
        "status": "success",
        "user": user
    })

@user_bp.route('/register', methods=['POST'])
def register_user():
    """Регистрация нового пользователя"""
    data = request.get_json()
    if not data or 'email' not in data or 'password' not in data:
        return jsonify({"status": "error", "message": "email and password required"}), 400
    if len(data['password']) < 6:
        return jsonify({"status": "error", "message": "Password must be at least 6 characters"}), 400
    
    # Получаем порядковый номер для player_id
    user_count = db.get_server_stats().get('total_users_count', 0)
    player_id = f"user_{user_count+1}"
    nick_name = data.get('nick_name') or f"Player {user_count+1}"
    email = data['email']
    password = data['password']
    avatar_url = data.get('avatar_url', "")
    mmr = json.dumps(data.get('mmr', [1000, 1000, 1000, 1000, 1000, 1000]))  # Начальные рейтинги для 6 типов матчей
    
    user_data = {
        'user_id': player_id,  # В БД используется user_id
        'nick_name': nick_name,
        'email': email,
        'password': password,
        'avatar_url': avatar_url,
        'mmr': mmr,
        'status': 'active',
        'profile_data': json.dumps({
            'match_current': None,
            'queue_ticket_id': None,
            'money': 0.0,
            'last_login_time': None,
            'last_logout_time': None,
            'last_login_ip': '',
            'last_logout_ip': '',
            'last_login_device': '',
            'last_logout_device': ''
        })
    }
    
    try:
        created = db.create_user(user_data)
        return jsonify({
            "status": "success", 
            "player_id": player_id, 
            "nick_name": nick_name,
            "user": created
        }), 201
    except ValueError as e:
        return jsonify({"status": "error", "message": str(e)}), 409

@user_bp.route('/', methods=['PUT'])
def update_user():
    """Обновляет данные пользователя"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id is required"}), 400
    
    updated_user = db.update_user(data)
    if not updated_user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    return jsonify({
        "status": "success",
        "message": "User updated successfully",
        "user": updated_user
    })

@user_bp.route('/', methods=['DELETE'])
def delete_user():
    """Удаляет пользователя"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id is required"}), 400
    
    deleted_user = db.delete_user(data['player_id'])
    if not deleted_user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    return jsonify({
        "status": "success",
        "message": "User deleted successfully",
        "user": deleted_user
    })

@user_bp.route('/find_by_email', methods=['GET'])
def find_user_by_email():
    """Найти пользователя по email"""
    email = request.args.get('email')
    if not email:
        return jsonify({"status": "error", "message": "Email parameter required"}), 400
    
    try:
        # Ищем пользователя в базе данных
        conn = db.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM users WHERE email = ?', (email,))
        user_row = cursor.fetchone()
        
        if user_row:
            user_data = dict(user_row)
            conn.close()
            
            return jsonify({
                "status": "success",
                "user": user_data
            })
        else:
            conn.close()
            return jsonify({
                "status": "error",
                "message": "User not found"
            }), 404
            
    except Exception as e:
        return jsonify({
            "status": "error",
            "message": f"Database error: {str(e)}"
        }), 500

@user_bp.route('/<player_id>', methods=['GET'])
def get_user_profile(player_id):
    """Получить профиль пользователя по player_id"""
    user = db.get_user(player_id)
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    return jsonify({"status": "success", "user": user})

@user_bp.route('/<player_id>', methods=['PUT'])
def update_user_profile(player_id):
    """Обновить профиль пользователя (никнейм, аватар)"""
    data = request.get_json()
    if not data:
        return jsonify({"status": "error", "message": "No data"}), 400
    update_data = {"user_id": player_id}
    if 'nick_name' in data:
        update_data['nick_name'] = data['nick_name']
    if 'avatar_url' in data:
        update_data['avatar_url'] = data['avatar_url']
    updated = db.update_user(update_data)
    if not updated:
        return jsonify({"status": "error", "message": "User not found"}), 404
    return jsonify({"status": "success", "user": updated})

@user_bp.route('/game_data/<player_id>', methods=['PUT'])
def update_game_data(player_id):
    """Обновить игровые данные пользователя (матч, очередь, MMR)"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data:
        return jsonify({"status": "error", "message": "No data"}), 400
    
    # Получаем текущий профиль
    user = db.get_user(player_id)
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    # Обновляем profile_data
    profile_data = json.loads(user.get('profile_data', '{}'))
    
    if 'match_current' in data:
        profile_data['match_current'] = data['match_current']
    if 'queue_ticket_id' in data:
        profile_data['queue_ticket_id'] = data['queue_ticket_id']
    if 'money' in data:
        profile_data['money'] = data['money']
    if 'last_login_time' in data:
        profile_data['last_login_time'] = data['last_login_time']
    if 'last_logout_time' in data:
        profile_data['last_logout_time'] = data['last_logout_time']
    if 'last_login_ip' in data:
        profile_data['last_login_ip'] = data['last_login_ip']
    if 'last_logout_ip' in data:
        profile_data['last_logout_ip'] = data['last_logout_ip']
    if 'last_login_device' in data:
        profile_data['last_login_device'] = data['last_login_device']
    if 'last_logout_device' in data:
        profile_data['last_logout_device'] = data['last_logout_device']
    
    update_data = {
        "user_id": player_id,
        "profile_data": json.dumps(profile_data)
    }
    
    # Обновляем MMR если передан
    if 'mmr' in data:
        update_data['mmr'] = json.dumps(data['mmr'])
    
    updated = db.update_user(update_data)
    if not updated:
        return jsonify({"status": "error", "message": "Failed to update user"}), 500
    
    return jsonify({"status": "success", "user": updated})

@user_bp.route('/login', methods=['POST'])
def login_user():
    """Авторизация пользователя"""
    data = request.get_json()
    if not data or 'email' not in data or 'password' not in data:
        return jsonify({"status": "error", "message": "email and password required"}), 400
    
    # Найти пользователя по email
    user = db.get_user_by_email(data['email'])
    if not user:
        return jsonify({"status": "error", "message": "Invalid credentials"}), 401
    
    # Проверить пароль (в продакшене надо использовать хэширование)
    if user['password'] != data['password']:
        return jsonify({"status": "error", "message": "Invalid credentials"}), 401
    
    # Обновить время последнего входа
    import datetime
    now = datetime.datetime.now().isoformat()
    profile_data = json.loads(user.get('profile_data', '{}'))
    profile_data['last_login_time'] = now
    profile_data['last_login_ip'] = request.remote_addr
    profile_data['last_login_device'] = request.headers.get('User-Agent', '')
    
    db.update_user({
        "user_id": user['user_id'],
        "profile_data": json.dumps(profile_data)
    })
    
    return jsonify({
        "status": "success",
        "player_id": user['user_id'],
        "nick_name": user['nick_name'],
        "user": user
    })

@user_bp.route('/logout', methods=['POST'])
def logout_user():
    """Выход пользователя из системы"""
    data = request.get_json()
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id required"}), 400
    
    user = db.get_user(data['player_id'])
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    # Обновить время последнего выхода
    import datetime
    now = datetime.datetime.now().isoformat()
    profile_data = json.loads(user.get('profile_data', '{}'))
    profile_data['last_logout_time'] = now
    profile_data['last_logout_ip'] = request.remote_addr
    profile_data['last_logout_device'] = request.headers.get('User-Agent', '')
    
    db.update_user({
        "user_id": user['user_id'],
        "profile_data": json.dumps(profile_data)
    })
    
    return jsonify({"status": "success", "message": "Logged out successfully"}) 