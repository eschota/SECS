from flask import Blueprint, request, jsonify
from static import verify_admin_token
import json
import uuid
from datetime import datetime
from typing import Dict, List, Optional
from database import db

tasks_bp = Blueprint('tasks', __name__)

# Простое хранилище задач в памяти
tasks_queue = []
completed_tasks = []

@tasks_bp.route('/api-get-tasks', methods=['POST'])
def get_tasks():
    """Получение задач для парсинга"""
    try:
        data = request.get_json()
        if not data:
            return jsonify({"error": "No data provided"}), 400
        
        nick = data.get('nick', 'unknown')
        client_version = data.get('client_version', 'unknown')
        task_type = data.get('task_type', 0)
        
        # Проверяем, есть ли задачи в очереди
        if not tasks_queue:
            return "", 204  # No content - нет задач
        
        # Берем первую задачу из очереди
        task = tasks_queue.pop(0)
        
        # Логируем запрос
        print(f"Task assigned to {nick} (v{client_version}): {task.get('task_id', 'unknown')}")
        
        # Если в запросе есть результат предыдущей задачи, сохраняем его
        if 'result' in data or 'html' in data:
            completed_task = {
                'task_id': data.get('task_id'),
                'status': data.get('status'),
                'result': data.get('result'),
                'html': data.get('html', ''),
                'nick': nick,
                'completed_at': datetime.now().isoformat()
            }
            completed_tasks.append(completed_task)
            print(f"Task completed by {nick}: {completed_task['task_id']}")
        
        return jsonify(task)
        
    except Exception as e:
        print(f"Error in get_tasks: {e}")
        return jsonify({"error": "Internal server error"}), 500

@tasks_bp.route('/api-add-task', methods=['POST'])
def add_task():
    """Добавление задачи для парсинга (только для админа)"""
    if not verify_admin_token():
        return jsonify({"error": "Unauthorized"}), 401
    
    try:
        data = request.get_json()
        if not data:
            return jsonify({"error": "No data provided"}), 400
        
        task = {
            'task_id': str(uuid.uuid4()),
            'task_type': data.get('task_type', 'parse'),
            'url': data.get('url', ''),
            'script': data.get('script', ''),
            'page_id': data.get('page_id', ''),
            'page_type': data.get('page_type', ''),
            'page_url_file_on_server': data.get('page_url_file_on_server'),
            'created_at': datetime.now().isoformat(),
            'priority': data.get('priority', 0)
        }
        
        # Добавляем задачу в очередь
        tasks_queue.append(task)
        tasks_queue.sort(key=lambda x: x.get('priority', 0), reverse=True)
        
        return jsonify({
            "status": "success",
            "task_id": task['task_id'],
            "queue_position": len(tasks_queue)
        })
        
    except Exception as e:
        print(f"Error in add_task: {e}")
        return jsonify({"error": "Internal server error"}), 500

@tasks_bp.route('/api-task-status', methods=['GET'])
def get_task_status():
    """Получение статуса задач"""
    return jsonify({
        "status": "success",
        "queue_size": len(tasks_queue),
        "completed_tasks": len(completed_tasks),
        "last_completed": completed_tasks[-10:] if completed_tasks else []
    })

@tasks_bp.route('/api-clear-tasks', methods=['POST'])
def clear_tasks():
    """Очистка очереди задач (только для админа)"""
    if not verify_admin_token():
        return jsonify({"error": "Unauthorized"}), 401
    
    global tasks_queue, completed_tasks
    tasks_queue.clear()
    completed_tasks.clear()
    
    return jsonify({"status": "success", "message": "Tasks cleared"}) 