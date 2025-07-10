import sqlite3
import os
from datetime import datetime
from typing import List, Dict, Optional, Any

class GameDatabase:
    def __init__(self, db_path: str = "game_server.db"):
        """Инициализация базы данных"""
        self.db_path = db_path
        self.init_database()
    
    def get_connection(self):
        """Получение соединения с базой данных"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row  # Позволяет обращаться к колонкам по имени
        return conn
    
    def init_database(self):
        """Инициализация таблиц базы данных"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Таблица пользователей
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id TEXT UNIQUE NOT NULL,
                nick_name TEXT NOT NULL,
                email TEXT NOT NULL,
                password TEXT NOT NULL,
                avatar_url TEXT DEFAULT '',
                mmr TEXT DEFAULT '[]',
                status TEXT DEFAULT 'active',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_login TIMESTAMP,
                profile_data TEXT DEFAULT '{}'
            )
        ''')
        
        # Таблица пользователей в лобби
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS lobby_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id TEXT UNIQUE NOT NULL,
                username TEXT NOT NULL,
                status TEXT DEFAULT 'active',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_seen TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (user_id)
            )
        ''')
        
        # Таблица очереди
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS queue_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id TEXT UNIQUE NOT NULL,
                username TEXT NOT NULL,
                joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                priority INTEGER DEFAULT 0,
                status TEXT DEFAULT 'waiting',
                FOREIGN KEY (user_id) REFERENCES users (user_id)
            )
        ''')
        
        # Таблица игр
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS matches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                match_id TEXT UNIQUE NOT NULL,
                name TEXT NOT NULL,
                status TEXT DEFAULT 'waiting',
                max_players INTEGER DEFAULT 4,
                current_players INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                started_at TIMESTAMP,
                ended_at TIMESTAMP,
                players TEXT DEFAULT '[]'
            )
        ''')
        
        # Таблица игровых сессий
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS game_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                match_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                left_at TIMESTAMP,
                status TEXT DEFAULT 'active',
                FOREIGN KEY (match_id) REFERENCES matches (match_id),
                FOREIGN KEY (user_id) REFERENCES users (user_id)
            )
        ''')
        
        # Таблица статистики игр
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS game_stats (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id TEXT NOT NULL,
                matches_played INTEGER DEFAULT 0,
                matches_won INTEGER DEFAULT 0,
                total_score INTEGER DEFAULT 0,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (user_id)
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def dict_from_row(self, row) -> Dict[str, Any]:
        """Преобразование строки БД в словарь"""
        if row is None:
            return {}
        return dict(row)
    
    # Методы для работы с пользователями
    def create_user(self, user_data: Dict[str, Any]) -> Dict[str, Any]:
        """Создание нового пользователя"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute('''
                INSERT INTO users (user_id, nick_name, email, password, avatar_url, mmr, status, profile_data)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                user_data['user_id'],
                user_data['nick_name'],
                user_data['email'],
                user_data['password'],
                user_data.get('avatar_url', ''),
                user_data.get('mmr', '[]'),
                user_data.get('status', 'active'),
                user_data.get('profile_data', '{}')
            ))
            
            conn.commit()
            return self.get_user(user_data['user_id'])
        except sqlite3.IntegrityError:
            raise ValueError("User already exists")
        finally:
            conn.close()
    
    def get_user(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Получение пользователя по ID"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM users WHERE user_id = ?', (user_id,))
        row = cursor.fetchone()
        conn.close()
        
        return self.dict_from_row(row)
    
    def get_user_by_email(self, email: str) -> Optional[Dict[str, Any]]:
        """Получение пользователя по email"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM users WHERE email = ?', (email,))
        row = cursor.fetchone()
        conn.close()
        
        return self.dict_from_row(row)
    
    def update_user(self, user_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Обновление данных пользователя"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Строим динамический UPDATE запрос
        fields = []
        values = []
        
        for key, value in user_data.items():
            if key != 'user_id' and key in ['nick_name', 'email', 'password', 'avatar_url', 'mmr', 'status', 'profile_data']:
                fields.append(f"{key} = ?")
                values.append(value)
        
        if not fields:
            conn.close()
            return None
        
        values.append(user_data['user_id'])
        query = f"UPDATE users SET {', '.join(fields)} WHERE user_id = ?"
        
        cursor.execute(query, values)
        conn.commit()
        conn.close()
        
        return self.get_user(user_data['user_id'])
    
    def delete_user(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Удаление пользователя"""
        user = self.get_user(user_id)
        if not user:
            return None
        
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Удаляем из всех связанных таблиц
        cursor.execute('DELETE FROM lobby_users WHERE user_id = ?', (user_id,))
        cursor.execute('DELETE FROM queue_users WHERE user_id = ?', (user_id,))
        cursor.execute('DELETE FROM game_sessions WHERE user_id = ?', (user_id,))
        cursor.execute('DELETE FROM game_stats WHERE user_id = ?', (user_id,))
        cursor.execute('DELETE FROM users WHERE user_id = ?', (user_id,))
        
        conn.commit()
        conn.close()
        
        return user
    
    # Методы для работы с лобби
    def create_lobby_user(self, user_data: Dict[str, Any]) -> Dict[str, Any]:
        """Создание пользователя в лобби"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute('''
                INSERT INTO lobby_users (user_id, username, status)
                VALUES (?, ?, ?)
            ''', (
                user_data['user_id'],
                user_data['username'],
                user_data.get('status', 'active')
            ))
            
            conn.commit()
            return self.get_lobby_user(user_data['user_id'])
        except sqlite3.IntegrityError:
            raise ValueError("User already in lobby")
        finally:
            conn.close()
    
    def get_lobby_user(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Получение пользователя из лобби"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM lobby_users WHERE user_id = ?', (user_id,))
        row = cursor.fetchone()
        conn.close()
        
        return self.dict_from_row(row)
    
    def get_lobby_users(self, page: int = 1, per_page: int = 1000) -> Dict[str, Any]:
        """Получение списка пользователей в лобби с пагинацией"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Общее количество пользователей
        cursor.execute('SELECT COUNT(*) FROM lobby_users')
        total_users = cursor.fetchone()[0]
        
        # Пользователи для текущей страницы
        offset = (page - 1) * per_page
        cursor.execute('''
            SELECT * FROM lobby_users 
            ORDER BY created_at DESC 
            LIMIT ? OFFSET ?
        ''', (per_page, offset))
        
        users = [self.dict_from_row(row) for row in cursor.fetchall()]
        conn.close()
        
        return {
            "users": users,
            "total_users": total_users,
            "page": page,
            "per_page": per_page,
            "total_pages": (total_users + per_page - 1) // per_page
        }
    
    def update_lobby_user(self, user_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Обновление пользователя в лобби"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        fields = []
        values = []
        
        for key, value in user_data.items():
            if key != 'user_id' and key in ['username', 'status']:
                fields.append(f"{key} = ?")
                values.append(value)
        
        if not fields:
            conn.close()
            return None
        
        values.append(user_data['user_id'])
        query = f"UPDATE lobby_users SET {', '.join(fields)} WHERE user_id = ?"
        
        cursor.execute(query, values)
        conn.commit()
        conn.close()
        
        return self.get_lobby_user(user_data['user_id'])
    
    def delete_lobby_user(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Удаление пользователя из лобби"""
        user = self.get_lobby_user(user_id)
        if not user:
            return None
        
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('DELETE FROM lobby_users WHERE user_id = ?', (user_id,))
        conn.commit()
        conn.close()
        
        return user
    
    # Методы для работы с очередью
    def add_user_to_queue(self, user_data: Dict[str, Any]) -> Dict[str, Any]:
        """Добавление пользователя в очередь"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute('''
                INSERT INTO queue_users (user_id, username, priority, status)
                VALUES (?, ?, ?, ?)
            ''', (
                user_data['user_id'],
                user_data['username'],
                user_data.get('priority', 0),
                user_data.get('status', 'waiting')
            ))
            
            conn.commit()
            return self.get_queue_user(user_data['user_id'])
        except sqlite3.IntegrityError:
            raise ValueError("User already in queue")
        finally:
            conn.close()
    
    def get_queue_user(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Получение пользователя из очереди"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM queue_users WHERE user_id = ?', (user_id,))
        row = cursor.fetchone()
        conn.close()
        
        return self.dict_from_row(row)
    
    def get_queue_users(self) -> Dict[str, Any]:
        """Получение списка пользователей в очереди"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM queue_users ORDER BY priority DESC, joined_at ASC')
        users = [self.dict_from_row(row) for row in cursor.fetchall()]
        
        conn.close()
        
        return {
            "users": users,
            "total_users": len(users)
        }
    
    def remove_user_from_queue(self, user_id: str) -> Optional[Dict[str, Any]]:
        """Удаление пользователя из очереди"""
        user = self.get_queue_user(user_id)
        if not user:
            return None
        
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('DELETE FROM queue_users WHERE user_id = ?', (user_id,))
        conn.commit()
        conn.close()
        
        return user
    
    # Методы для работы с играми
    def create_game(self, game_data: Dict[str, Any]) -> Dict[str, Any]:
        """Создание новой игры"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute('''
                INSERT INTO matches (match_id, name, status, max_players, current_players, players)
                VALUES (?, ?, ?, ?, ?, ?)
            ''', (
                game_data['match_id'],
                game_data['name'],
                game_data.get('status', 'waiting'),
                game_data.get('max_players', 4),
                game_data.get('current_players', 0),
                game_data.get('players', '[]')
            ))
            
            conn.commit()
            return self.get_game(game_data['match_id'])
        except sqlite3.IntegrityError:
            raise ValueError("Match already exists")
        finally:
            conn.close()
    
    def get_game(self, match_id: str) -> Optional[Dict[str, Any]]:
        """Получение матча по ID"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM matches WHERE match_id = ?', (match_id,))
        row = cursor.fetchone()
        conn.close()
        
        return self.dict_from_row(row)
    
    def get_matches(self) -> Dict[str, Any]:
        """Получение списка всех игр"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        cursor.execute('SELECT * FROM matches ORDER BY created_at DESC')
        matches = [self.dict_from_row(row) for row in cursor.fetchall()]
        
        conn.close()
        
        return {
            "matches": matches,
            "total_matches": len(matches)
        }
    
    def update_game(self, game_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Обновление данных игры"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        fields = []
        values = []
        
        for key, value in game_data.items():
            if key != 'match_id' and key in ['name', 'status', 'max_players', 'current_players', 'players']:
                fields.append(f"{key} = ?")
                values.append(value)
        
        if not fields:
            conn.close()
            return None
        
        values.append(game_data['match_id'])
        query = f"UPDATE matches SET {', '.join(fields)} WHERE match_id = ?"
        
        cursor.execute(query, values)
        conn.commit()
        conn.close()
        
        return self.get_game(game_data['match_id'])
    
    def delete_game(self, match_id: str) -> Optional[Dict[str, Any]]:
        """Удаление игры"""
        game = self.get_game(match_id)
        if not game:
            return None
        
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Удаляем связанные записи
        cursor.execute('DELETE FROM game_sessions WHERE match_id = ?', (match_id,))
        cursor.execute('DELETE FROM matches WHERE match_id = ?', (match_id,))
        
        conn.commit()
        conn.close()
        
        return game
    
    # Методы для статистики
    def get_server_stats(self) -> Dict[str, Any]:
        """Получение статистики сервера"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Количество пользователей в лобби
        cursor.execute('SELECT COUNT(*) FROM lobby_users')
        lobby_users_count = cursor.fetchone()[0]
        
        # Количество пользователей в очереди
        cursor.execute('SELECT COUNT(*) FROM queue_users')
        queue_users_count = cursor.fetchone()[0]
        
        # Количество активных игр
        cursor.execute('SELECT COUNT(*) FROM matches WHERE status = "active"')
        active_matches_count = cursor.fetchone()[0]
        
        # Общее количество пользователей
        cursor.execute('SELECT COUNT(*) FROM users')
        total_users_count = cursor.fetchone()[0]
        
        conn.close()
        
        return {
            "lobby_users_count": lobby_users_count,
            "queue_users_count": queue_users_count,
            "active_matches_count": active_matches_count,
            "total_users_count": total_users_count
        }
    
    def cleanup_old_data(self, days: int = 30):
        """Очистка старых данных"""
        conn = self.get_connection()
        cursor = conn.cursor()
        
        # Удаляем старые игровые сессии
        cursor.execute('''
            DELETE FROM game_sessions 
            WHERE left_at IS NOT NULL 
            AND left_at < datetime('now', '-{} days')
        '''.format(days))
        
        # Удаляем завершенные игры старше указанного периода
        cursor.execute('''
            DELETE FROM matches 
            WHERE ended_at IS NOT NULL 
            AND ended_at < datetime('now', '-{} days')
        '''.format(days))
        
        conn.commit()
        conn.close()

# Глобальный экземпляр базы данных
db = GameDatabase() 