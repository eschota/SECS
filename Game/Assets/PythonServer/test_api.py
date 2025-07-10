#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Скрипт для тестирования API endpoints игрового сервера
"""

import requests
import json
import time
import sys

# Настройки
API_BASE_URL = "https://renderfin.com"
ADMIN_TOKEN = "ZXCVBNM,1234567890"

# Заголовки для админских запросов
ADMIN_HEADERS = {
    'Content-Type': 'application/json',
    'Authorization': f'Bearer {ADMIN_TOKEN}'
}

# Обычные заголовки
HEADERS = {
    'Content-Type': 'application/json'
}

def test_endpoint(method, url, data=None, headers=None, expected_status=200):
    """Тестирует endpoint"""
    try:
        print(f"\n🔍 Testing {method} {url}")
        
        if method == 'GET':
            response = requests.get(url, headers=headers, timeout=10)
        elif method == 'POST':
            response = requests.post(url, json=data, headers=headers, timeout=10)
        elif method == 'PUT':
            response = requests.put(url, json=data, headers=headers, timeout=10)
        elif method == 'DELETE':
            response = requests.delete(url, json=data, headers=headers, timeout=10)
        
        print(f"Status: {response.status_code}")
        
        if response.status_code == expected_status:
            print("✅ SUCCESS")
            try:
                result = response.json()
                print(f"Response: {json.dumps(result, indent=2)}")
                return result
            except:
                print(f"Response: {response.text}")
                return response.text
        else:
            print(f"❌ FAILED - Expected {expected_status}, got {response.status_code}")
            try:
                error = response.json()
                print(f"Error: {json.dumps(error, indent=2)}")
            except:
                print(f"Error: {response.text}")
            return None
            
    except Exception as e:
        print(f"❌ EXCEPTION: {e}")
        return None

def test_server_basic():
    """Тестирует базовые endpoints сервера"""
    print("\n" + "="*60)
    print("🚀 TESTING BASIC SERVER ENDPOINTS")
    print("="*60)
    
    # Тестируем главную страницу
    test_endpoint('GET', f"{API_BASE_URL}/")
    
    # Тестируем статус лобби
    test_endpoint('GET', f"{API_BASE_URL}/api-game-lobby/")

def test_user_endpoints():
    """Тестирует пользовательские endpoints"""
    print("\n" + "="*60)
    print("👤 TESTING USER ENDPOINTS")
    print("="*60)
    
    # Создаем тестового пользователя
    test_user_data = {
        "email": "test@example.com",
        "password": "testpass123",
        "nick_name": "TestPlayer"
    }
    
    user_result = test_endpoint('POST', f"{API_BASE_URL}/api-game-user/register", test_user_data, HEADERS, 201)
    
    if user_result:
        player_id = user_result.get('player_id')
        print(f"Created test user: {player_id}")
        
        # Тестируем получение пользователя
        test_endpoint('GET', f"{API_BASE_URL}/api-game-user/{player_id}", headers=HEADERS)
        
        # Тестируем авторизацию
        login_data = {
            "email": "test@example.com",
            "password": "testpass123"
        }
        test_endpoint('POST', f"{API_BASE_URL}/api-game-user/login", login_data, HEADERS)
        
        return player_id
    
    return None

def test_lobby_endpoints(player_id):
    """Тестирует lobby endpoints"""
    print("\n" + "="*60)
    print("🏛️ TESTING LOBBY ENDPOINTS")
    print("="*60)
    
    # Тестируем присоединение к лобби
    join_data = {"player_id": player_id}
    test_endpoint('POST', f"{API_BASE_URL}/api-game-lobby/join", join_data, HEADERS)
    
    # Тестируем получение пользователей лобби
    test_endpoint('GET', f"{API_BASE_URL}/api-game-lobby/users", headers=HEADERS)
    
    # Тестируем выход из лобби
    test_endpoint('POST', f"{API_BASE_URL}/api-game-lobby/leave", join_data, HEADERS)

def test_queue_endpoints(player_id):
    """Тестирует queue endpoints"""
    print("\n" + "="*60)
    print("⏱️ TESTING QUEUE ENDPOINTS")
    print("="*60)
    
    # Тестируем получение очередей
    test_endpoint('GET', f"{API_BASE_URL}/api-game-queue/", headers=HEADERS)
    
    # Тестируем добавление в очередь
    queue_data = {
        "queue_match_type": 0,  # 1v1
        "player_id": player_id
    }
    test_endpoint('POST', f"{API_BASE_URL}/api-game-queue/", queue_data, HEADERS)
    
    # Тестируем статус игрока в очереди
    test_endpoint('GET', f"{API_BASE_URL}/api-game-queue/player/{player_id}", headers=HEADERS)
    
    # Даем время на матчмейкинг
    print("\n⏳ Waiting for potential matchmaking...")
    time.sleep(5)
    
    # Тестируем удаление из очереди
    remove_data = {"player_id": player_id}
    test_endpoint('DELETE', f"{API_BASE_URL}/api-game-queue/", remove_data, HEADERS)

def test_match_endpoints():
    """Тестирует match endpoints"""
    print("\n" + "="*60)
    print("🎮 TESTING MATCH ENDPOINTS")
    print("="*60)
    
    # Тестируем получение матчей
    test_endpoint('GET', f"{API_BASE_URL}/api-game-match/", headers=HEADERS)
    
    # Создаем тестовый матч (требует админские права)
    match_data = {
        "match_type": "1v1",
        "players": ["user_1", "user_2"]
    }
    match_result = test_endpoint('POST', f"{API_BASE_URL}/api-game-match/", match_data, ADMIN_HEADERS, 201)
    
    if match_result:
        match_id = match_result.get('match_id')
        print(f"Created test match: {match_id}")
        
        # Тестируем получение конкретного матча
        test_endpoint('GET', f"{API_BASE_URL}/api-game-match/{match_id}", headers=HEADERS)
        
        # Тестируем добавление действия в матч
        action_data = {
            "player_id": "user_1",
            "action_type": "test_action",
            "action_data": {"test": "data"}
        }
        test_endpoint('POST', f"{API_BASE_URL}/api-game-match/{match_id}/action", action_data, HEADERS)
        
        # Тестируем завершение матча
        finish_data = {
            "winners": ["user_1"],
            "losers": ["user_2"]
        }
        test_endpoint('POST', f"{API_BASE_URL}/api-game-match/{match_id}/finish", finish_data, ADMIN_HEADERS)

def test_admin_endpoints():
    """Тестирует админские endpoints"""
    print("\n" + "="*60)
    print("🔐 TESTING ADMIN ENDPOINTS")
    print("="*60)
    
    # Тестируем обновление настроек очереди
    queue_settings = {
        "mmr_min_limit_threshold": 30,
        "mmr_time_in_seconds_to_raise_threshold": 15,
        "mmr_raise_threshold_step": 0.15
    }
    test_endpoint('PUT', f"{API_BASE_URL}/api-game-queue/", queue_settings, ADMIN_HEADERS)
    
    # Тестируем очистку очереди
    test_endpoint('POST', f"{API_BASE_URL}/api-game-queue/clear", {}, ADMIN_HEADERS)

def main():
    """Основная функция тестирования"""
    print("🎯 GAME SERVER API TESTING SUITE")
    print("="*60)
    print(f"Target Server: {API_BASE_URL}")
    print("="*60)
    
    try:
        # Тестируем базовые endpoints
        test_server_basic()
        
        # Тестируем пользовательские endpoints
        player_id = test_user_endpoints()
        
        if player_id:
            # Тестируем lobby endpoints
            test_lobby_endpoints(player_id)
            
            # Тестируем queue endpoints
            test_queue_endpoints(player_id)
        
        # Тестируем match endpoints
        test_match_endpoints()
        
        # Тестируем админские endpoints
        test_admin_endpoints()
        
        print("\n" + "="*60)
        print("🎉 TESTING COMPLETED!")
        print("="*60)
        
    except KeyboardInterrupt:
        print("\n❌ Testing interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 