from flask import Blueprint, request, jsonify
from database import db
from static import verify_admin_token
import json

lobby_bp = Blueprint('lobby', __name__)

@lobby_bp.route('/', methods=['GET'])
def get_server_status():
    """Возвращает статус сервера"""
    stats = db.get_server_stats()
    return jsonify({
        "status": "success",
        "message": "Game Server is running",
        "server_info": {
            "port": 3329,
            "environment": "production",
            "url": "https://renderfin.com"
        },
        "stats": stats
    })

@lobby_bp.route('/users', methods=['GET'])
def get_lobby_users():
    """Возвращает список пользователей в лобби с пагинацией"""
    page = int(request.args.get('page', 1))
    per_page = int(request.args.get('per_page', 1000))
    
    # Ограничиваем размер страницы
    per_page = min(per_page, 1000)
    
    users_data = db.get_lobby_users(page, per_page)
    return jsonify({
        "status": "success",
        "data": users_data
    })

@lobby_bp.route('/users', methods=['POST'])
def create_lobby_user():
    """Создает нового пользователя в лобби"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'user_id' not in data or 'username' not in data:
        return jsonify({"status": "error", "message": "user_id and username required"}), 400
    
    # Проверяем, существует ли пользователь в основной таблице
    user = db.get_user(data['user_id'])
    if not user:
        return jsonify({"status": "error", "message": "User not found in main database"}), 404
    
    try:
        lobby_user = db.create_lobby_user({
            'user_id': data['user_id'],
            'username': data['username'],
            'status': data.get('status', 'active')
        })
        return jsonify({
            "status": "success",
            "user": lobby_user
        }), 201
    except ValueError as e:
        return jsonify({"status": "error", "message": str(e)}), 409

@lobby_bp.route('/users', methods=['PUT'])
def update_lobby_user():
    """Обновляет данные пользователя в лобби"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'user_id' not in data:
        return jsonify({"status": "error", "message": "user_id required"}), 400
    
    updated_user = db.update_lobby_user(data)
    if not updated_user:
        return jsonify({"status": "error", "message": "User not found in lobby"}), 404
    
    return jsonify({
        "status": "success",
        "user": updated_user
    })

@lobby_bp.route('/users', methods=['DELETE'])
def delete_lobby_user():
    """Удаляет пользователя из лобби"""
    if not verify_admin_token():
        return jsonify({"status": "error", "message": "Unauthorized"}), 401
    
    data = request.get_json()
    if not data or 'user_id' not in data:
        return jsonify({"status": "error", "message": "user_id required"}), 400
    
    deleted_user = db.delete_lobby_user(data['user_id'])
    if not deleted_user:
        return jsonify({"status": "error", "message": "User not found in lobby"}), 404
    
    return jsonify({
        "status": "success",
        "message": "User removed from lobby",
        "user": deleted_user
    })

@lobby_bp.route('/users/<user_id>', methods=['GET'])
def get_lobby_user(user_id):
    """Получает данные конкретного пользователя в лобби"""
    user = db.get_lobby_user(user_id)
    if not user:
        return jsonify({"status": "error", "message": "User not found in lobby"}), 404
    
    return jsonify({
        "status": "success",
        "user": user
    })

@lobby_bp.route('/join', methods=['POST'])
def join_lobby():
    """Добавляет пользователя в лобби"""
    data = request.get_json()
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id required"}), 400
    
    # Проверяем, существует ли пользователь
    user = db.get_user(data['player_id'])
    if not user:
        return jsonify({"status": "error", "message": "User not found"}), 404
    
    try:
        lobby_user = db.create_lobby_user({
            'user_id': data['player_id'],
            'username': user['nick_name'],
            'status': 'active'
        })
        return jsonify({
            "status": "success",
            "message": "Joined lobby successfully",
            "user": lobby_user
        }), 201
    except ValueError as e:
        return jsonify({"status": "error", "message": str(e)}), 409

@lobby_bp.route('/leave', methods=['POST'])
def leave_lobby():
    """Удаляет пользователя из лобби"""
    data = request.get_json()
    if not data or 'player_id' not in data:
        return jsonify({"status": "error", "message": "player_id required"}), 400
    
    deleted_user = db.delete_lobby_user(data['player_id'])
    if not deleted_user:
        return jsonify({"status": "error", "message": "User not found in lobby"}), 404
    
    return jsonify({
        "status": "success",
        "message": "Left lobby successfully",
        "user": deleted_user
    })
