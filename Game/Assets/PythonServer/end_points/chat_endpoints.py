from flask import Blueprint, request, jsonify
from database import db
import json
from datetime import datetime

chat_bp = Blueprint('chat', __name__)

@chat_bp.route('/', methods=['GET'])
def get_chat_messages():
    """Получить последние сообщения чата"""
    limit = request.args.get('limit', 50, type=int)
    offset = request.args.get('offset', 0, type=int)
    
    try:
        conn = db.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT cm.*, u.nick_name, u.avatar_url 
            FROM chat_messages cm
            JOIN users u ON cm.user_id = u.user_id
            ORDER BY cm.created_at DESC
            LIMIT ? OFFSET ?
        ''', (limit, offset))
        
        messages = []
        for row in cursor.fetchall():
            message_data = dict(row)
            messages.append(message_data)
        
        conn.close()
        
        # Возвращаем в хронологическом порядке (старые сверху)
        messages.reverse()
        
        return jsonify({
            "status": "success",
            "messages": messages,
            "total": len(messages)
        })
        
    except Exception as e:
        return jsonify({
            "status": "error", 
            "message": f"Database error: {str(e)}"
        }), 500

@chat_bp.route('/', methods=['POST'])
def send_chat_message():
    """Отправить сообщение в чат"""
    data = request.get_json()
    
    if not data or 'user_id' not in data or 'message' not in data:
        return jsonify({
            "status": "error", 
            "message": "user_id and message are required"
        }), 400
    
    user_id = data['user_id']
    message = data['message'].strip()
    
    if not message:
        return jsonify({
            "status": "error", 
            "message": "Message cannot be empty"
        }), 400
    
    if len(message) > 500:
        return jsonify({
            "status": "error", 
            "message": "Message too long (max 500 characters)"
        }), 400
    
    # Проверяем, что пользователь существует
    user = db.get_user(user_id)
    if not user:
        return jsonify({
            "status": "error", 
            "message": "User not found"
        }), 404
    
    try:
        conn = db.get_connection()
        cursor = conn.cursor()
        
        # Добавляем сообщение в БД
        cursor.execute('''
            INSERT INTO chat_messages (user_id, message, message_type, created_at)
            VALUES (?, ?, ?, ?)
        ''', (user_id, message, 'text', datetime.now().isoformat()))
        
        message_id = cursor.lastrowid
        conn.commit()
        
        # Получаем полную информацию о созданном сообщении
        cursor.execute('''
            SELECT cm.*, u.nick_name, u.avatar_url 
            FROM chat_messages cm
            JOIN users u ON cm.user_id = u.user_id
            WHERE cm.id = ?
        ''', (message_id,))
        
        created_message = dict(cursor.fetchone())
        conn.close()
        
        return jsonify({
            "status": "success",
            "message": created_message
        }), 201
        
    except Exception as e:
        return jsonify({
            "status": "error", 
            "message": f"Database error: {str(e)}"
        }), 500

@chat_bp.route('/system', methods=['POST'])
def send_system_message():
    """Отправить системное сообщение (подключение/отключение игрока)"""
    data = request.get_json()
    
    if not data or 'user_id' not in data or 'message' not in data:
        return jsonify({
            "status": "error", 
            "message": "user_id and message are required"
        }), 400
    
    user_id = data['user_id']
    message = data['message'].strip()
    
    # Проверяем, что пользователь существует
    user = db.get_user(user_id)
    if not user:
        return jsonify({
            "status": "error", 
            "message": "User not found"
        }), 404
    
    try:
        conn = db.get_connection()
        cursor = conn.cursor()
        
        # Добавляем системное сообщение в БД
        cursor.execute('''
            INSERT INTO chat_messages (user_id, message, message_type, created_at)
            VALUES (?, ?, ?, ?)
        ''', (user_id, message, 'system', datetime.now().isoformat()))
        
        message_id = cursor.lastrowid
        conn.commit()
        
        # Получаем полную информацию о созданном сообщении
        cursor.execute('''
            SELECT cm.*, u.nick_name, u.avatar_url 
            FROM chat_messages cm
            JOIN users u ON cm.user_id = u.user_id
            WHERE cm.id = ?
        ''', (message_id,))
        
        created_message = dict(cursor.fetchone())
        conn.close()
        
        return jsonify({
            "status": "success",
            "message": created_message
        }), 201
        
    except Exception as e:
        return jsonify({
            "status": "error", 
            "message": f"Database error: {str(e)}"
        }), 500

@chat_bp.route('/online_count', methods=['GET'])
def get_online_count():
    """Получить количество игроков онлайн"""
    try:
        # Получаем актуальную статистику
        stats = db.get_server_stats()
        online_count = stats.get('lobby_users_count', 0)
        
        print(f"Online count request: {online_count} users in lobby")
        
        return jsonify({
            "status": "success",
            "online_count": online_count
        })
    except Exception as e:
        return jsonify({
            "status": "error", 
            "message": f"Database error: {str(e)}"
        }), 500

@chat_bp.route('/heartbeat', methods=['POST'])
def heartbeat():
    """Обновить активность пользователя (heartbeat)"""
    data = request.get_json()
    print(f"Heartbeat request data: {data}")
    
    if not data or 'user_id' not in data:
        print(f"Heartbeat error: Missing user_id in data: {data}")
        return jsonify({
            "status": "error", 
            "message": "user_id is required"
        }), 400
    
    user_id = data['user_id']
    print(f"Heartbeat processing for user_id: {user_id}")
    
    try:
        conn = db.get_connection()
        cursor = conn.cursor()
        
        # Обновляем время последней активности пользователя в лобби
        cursor.execute('''
            UPDATE lobby_users 
            SET last_seen = CURRENT_TIMESTAMP 
            WHERE user_id = ?
        ''', (user_id,))
        
        print(f"Heartbeat: Updated {cursor.rowcount} rows for user {user_id}")
        
        if cursor.rowcount == 0:
            # Если пользователя нет в лобби, добавляем его
            user = db.get_user(user_id)
            if user:
                print(f"Heartbeat: Adding user {user_id} to lobby")
                cursor.execute('''
                    INSERT OR REPLACE INTO lobby_users (user_id, username, status, last_seen)
                    VALUES (?, ?, ?, CURRENT_TIMESTAMP)
                ''', (user_id, user['nick_name'], 'active'))
                print(f"Heartbeat: Successfully added user {user_id} to lobby")
            else:
                print(f"Heartbeat: User {user_id} not found in main users table")
        else:
            print(f"Heartbeat: Updated existing user {user_id} in lobby")
        
        conn.commit()
        conn.close()
        
        return jsonify({
            "status": "success",
            "message": "Heartbeat updated"
        })
        
    except Exception as e:
        return jsonify({
            "status": "error", 
            "message": f"Database error: {str(e)}"
        }), 500