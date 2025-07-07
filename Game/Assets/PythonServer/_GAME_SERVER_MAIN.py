#Техническое задание Базовое:
# вебсервер на flask для работы с игрой

# порт для работы сервера 3329 
# @https://renderfin.com/game/index.html - официальный и рабочий адрес игры настроенный для работы на этом сервере.

# этот скрипт отвечает только за маршрутизацию запросов, все endpoint должны быть в другом файле, каждый соответствует своему маршруту и должен лежать в папке /endpoints.
# токен админа - ZXCVBNM,1234567890 - для всех операций с пользователями.

# LOBBY:
#/api-lobby GET - возвращает статус сервера.
#/api-lobby-user GET - возвращает список пользователей в игре c пагинацией по дефолту 1000 пользователей на страницу.
#/api-lobby-user POST - создает нового пользователя в игре.
#/api-lobby-user PUT - обновляет данные пользователя в игре.
#/api-lobby-user DELETE - удаляет пользователя из игры.

# Queue:
#/api-queue GET - возвращает список пользователей в очереди.
#/api-queue POST - добавляет пользователя в очередь.
#/api-queue DELETE - удаляет пользователя из очереди.
#/api-queue PUT - обновляет данные очереди.

# Match:
#/api-matches GET - возвращает список игр.
#/api-match POST - создает новую игру.
#/api-match PUT - обновляет данные игры.
#/api-match DELETE - удаляет игру.

# User:

#/api-user GET - возвращает данные пользователя.
#/api-user POST - создает нового пользователя.
#/api-user PUT - обновляет данные пользователя.
#/api-user DELETE - удаляет пользователя.

#/online-game GET - возвращает html страницу с игрой.
#game.html, game.js, game.css - файлы для этой страницы.

# есть веб билд который доступен по @https://renderfin.com/game/index.html 
# и он лежит на этом сервере по абсолютному пути c:\NDLWebServerBuild\wwwroot\Game\index.html

#/unity-game GET - возвращает Unity WebGL игру (если есть).


from flask import Flask, request, jsonify, send_from_directory, redirect
from flask_cors import CORS
import os
import sys
import logging


# Добавляем папку endpoints в путь для импорта
sys.path.append(os.path.join(os.path.dirname(__file__), 'end_points'))

# Импортируем endpoint модули
from lobby_endpoints import lobby_bp
from queue_endpoints import queue_bp
from match_endpoints import match_bp
from user_endpoints import user_bp

app = Flask(__name__)
CORS(app)

# Регистрируем blueprint'ы с единым префиксом api-game-
app.register_blueprint(lobby_bp, url_prefix='/api-game-lobby')
app.register_blueprint(queue_bp, url_prefix='/api-game-queue')
app.register_blueprint(match_bp, url_prefix='/api-game-match')
app.register_blueprint(user_bp, url_prefix='/api-game-user')

STATIC_ONLINE_GAME_DIR = r"c:\\NDLWebServerBuild\\wwwroot\\online-game"

@app.route('/')
def home():
    return jsonify({
        "status": "success",
        "message": "Game Server is running",
        "port": 3329
    })

@app.route('/online-game/')
def online_game():
    """Возвращает HTML страницу с игрой"""
    logging.warning(f"Serving game.html from: {STATIC_ONLINE_GAME_DIR}")
    return send_from_directory(STATIC_ONLINE_GAME_DIR, 'game.html')

@app.route('/online-game/<path:filename>')
def static_files(filename):
    """Возвращает статические файлы для игры (css, js и др.)"""
    logging.warning(f"Serving static file: {filename} from: {STATIC_ONLINE_GAME_DIR}")
    return send_from_directory(STATIC_ONLINE_GAME_DIR, filename)

@app.route('/online-game/game.css')
def serve_game_css():
    return send_from_directory(STATIC_ONLINE_GAME_DIR, 'game.css')

@app.route('/online-game/game.js')
def serve_game_js():
    return send_from_directory(STATIC_ONLINE_GAME_DIR, 'game.js')

@app.route('/unity-game/')
def unity_game():
    """Возвращает Unity WebGL игру (index.html)"""
    unity_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
    unity_build_dir = os.path.join(unity_path, 'Build')
    index_path = os.path.join(unity_build_dir, 'index.html')
    if os.path.exists(index_path):
        return send_from_directory(unity_build_dir, 'index.html')
    else:
        return jsonify({
            "status": "info",
            "message": "Unity WebGL build not found. Please ensure Build/index.html exists in the project root.",
            "unity_path": unity_build_dir
        }), 404

@app.route('/unity-game/<path:filename>')
def unity_static_files(filename):
    """Возвращает статические файлы Unity игры (js, wasm, data и др.)"""
    unity_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
    unity_build_dir = os.path.join(unity_path, 'Build')
    file_path = os.path.join(unity_build_dir, filename)
    if os.path.exists(file_path):
        return send_from_directory(unity_build_dir, filename)
    else:
        # Также пробуем отдавать TemplateData и index.html из корня билда
        template_data_dir = os.path.join(unity_path, 'TemplateData')
        if os.path.exists(os.path.join(template_data_dir, filename)):
            return send_from_directory(template_data_dir, filename)
        if filename == 'index.html' and os.path.exists(os.path.join(unity_path, 'index.html')):
            return send_from_directory(unity_path, 'index.html')
        return jsonify({"error": "Unity build file not found"}), 404

if __name__ == '__main__':
    # Only lock the actual server process (child of reloader)
    import os, sys
    if os.environ.get('WERKZEUG_RUN_MAIN') == 'true':
        import signal, subprocess, atexit
        lock_file = os.path.join(os.path.dirname(__file__), 'server.lock')
        if os.path.exists(lock_file):
            try:
                with open(lock_file) as f:
                    pid = int(f.read())
                if os.name == 'nt': subprocess.run(['taskkill','/F','/PID',str(pid)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                else: os.kill(pid, signal.SIGTERM)
            except: pass
            try: os.remove(lock_file)
            except: pass
        with open(lock_file,'w') as f: f.write(str(os.getpid()))
        atexit.register(lambda: os.remove(lock_file) if os.path.exists(lock_file) else None)
    print("Starting Game Server on port 3329...")
    print("Available routes:")
    print("- /online-game/ - Game portal with authentication")
    print("- /unity-game/ - Unity WebGL game (if available)")
    print("- /api-game-* - API endpoints")
    
    # Очистка очереди игроков при старте сервера
    from database import db as _db
    try:
        print("Clearing queue_users table on startup...")
        conn = _db.get_connection()
        cursor = conn.cursor()
        cursor.execute('DELETE FROM queue_users')
        conn.commit()
        conn.close()
        print("Queue cleared.")
    except Exception as e:
        print(f"Error clearing queue on startup: {e}")
    # Проверяем наличие Unity билда
    unity_path = os.path.join(os.path.dirname(__file__), '..', '..')
    if os.path.exists(os.path.join(unity_path, 'index.html')):
        print(f"✓ Unity WebGL build found at: {unity_path}")
    else:
        print(f"⚠ Unity WebGL build not found at: {unity_path}")
        print("   Please ensure index.html exists in the project root")
    
    app.run(host='0.0.0.0', port=3329, debug=True)

