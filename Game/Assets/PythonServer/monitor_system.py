#!/usr/bin/env python3
"""
Монитор системы - показывает статистику работы сервера и ботов
"""

import requests
import json
import time
import os

LOCAL_URL = "http://localhost:3329"

def get_server_stats():
    """Получает статистику сервера"""
    try:
        # Статистика лобби
        response = requests.get(f"{LOCAL_URL}/api-game-lobby/", timeout=5)
        if response.status_code == 200:
            lobby_data = response.json()
            stats = lobby_data.get('stats', {})
            
            print(f"👥 Total users: {stats.get('total_users_count', 0)}")
            print(f"🏛️ Lobby users: {stats.get('lobby_users_count', 0)}")
            print(f"⏱️ Queue users: {stats.get('queue_users_count', 0)}")
            print(f"🎮 Active matches: {stats.get('active_matches_count', 0)}")
            
        # Детальная статистика очередей
        response = requests.get(f"{LOCAL_URL}/api-game-queue/", timeout=5)
        if response.status_code == 200:
            queue_data = response.json()
            queues = queue_data.get('queues', {})
            
            print("\n📊 QUEUE DETAILS:")
            for queue_id, queue_info in queues.items():
                match_type = queue_info.get('match_type')
                current = queue_info.get('current_players', 0)
                required = queue_info.get('players_required', 0)
                print(f"  {match_type}: {current}/{required} players")
                
        # Статистика матчей
        response = requests.get(f"{LOCAL_URL}/api-game-match/", timeout=5)
        if response.status_code == 200:
            match_data = response.json()
            active_matches = match_data.get('active_matches', [])
            history_matches = match_data.get('history_matches', [])
            
            print(f"\n🎮 MATCHES:")
            print(f"  Active: {len(active_matches)}")
            print(f"  History: {len(history_matches)}")
            
            if active_matches:
                print("  Active matches:")
                for match in active_matches[:3]:  # Показываем первые 3
                    duration = int(match.get('duration', 0))
                    players = len(match.get('players', []))
                    match_type = match.get('match_type')
                    print(f"    {match_type}: {players} players, {duration}s")
                    
    except Exception as e:
        print(f"❌ Error getting stats: {e}")

def monitor_bots():
    """Мониторинг ботов через статистику пользователей"""
    try:
        response = requests.get(f"{LOCAL_URL}/api-game-lobby/users?per_page=100", timeout=5)
        if response.status_code == 200:
            data = response.json()
            users_data = data.get('data', {})
            users = users_data.get('users', [])
            
            bot_users = [user for user in users if 'bot_' in user.get('user_id', '')]
            print(f"\n🤖 BOTS IN LOBBY: {len(bot_users)}")
            
            if bot_users:
                for bot in bot_users[:5]:  # Показываем первых 5 ботов
                    user_id = bot.get('user_id')
                    username = bot.get('username')
                    print(f"  {user_id} ({username})")
                    
    except Exception as e:
        print(f"❌ Error monitoring bots: {e}")

def main():
    """Основная функция мониторинга"""
    print("🔍 GAME SERVER MONITOR")
    print("="*50)
    print("Monitoring server and bots...")
    print("Press Ctrl+C to stop")
    print("="*50)
    
    try:
        while True:
            os.system('cls' if os.name == 'nt' else 'clear')  # Очищаем экран
            
            print("🔍 GAME SERVER MONITOR")
            print("="*50)
            print(f"Time: {time.strftime('%H:%M:%S')}")
            print()
            
            # Статистика сервера
            get_server_stats()
            
            # Мониторинг ботов
            monitor_bots()
            
            print("\n" + "="*50)
            print("Refreshing in 10 seconds... (Press Ctrl+C to stop)")
            
            time.sleep(10)
            
    except KeyboardInterrupt:
        print("\n👋 Monitoring stopped!")

if __name__ == "__main__":
    main() 