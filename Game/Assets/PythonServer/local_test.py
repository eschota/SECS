#!/usr/bin/env python3
"""
Простой локальный тест для проверки работы сервера
"""

import requests
import json
import time

# Локальный сервер
LOCAL_URL = "http://localhost:3329"
ADMIN_TOKEN = "ZXCVBNM,1234567890"

def test_local_server():
    """Тестирует локальный сервер"""
    print("🚀 TESTING LOCAL SERVER")
    print("="*40)
    
    try:
        # Тест главной страницы
        print("Testing main endpoint...")
        response = requests.get(f"{LOCAL_URL}/", timeout=5)
        print(f"Status: {response.status_code}")
        if response.status_code == 200:
            print("✅ Main endpoint works!")
            data = response.json()
            print(f"Response: {json.dumps(data, indent=2)}")
        else:
            print("❌ Main endpoint failed")
            
        # Тест лобби
        print("\nTesting lobby endpoint...")
        response = requests.get(f"{LOCAL_URL}/api-game-lobby/", timeout=5)
        print(f"Status: {response.status_code}")
        if response.status_code == 200:
            print("✅ Lobby endpoint works!")
            data = response.json()
            print(f"Response: {json.dumps(data, indent=2)}")
        else:
            print("❌ Lobby endpoint failed")
            
        # Тест очереди
        print("\nTesting queue endpoint...")
        response = requests.get(f"{LOCAL_URL}/api-game-queue/", timeout=5)
        print(f"Status: {response.status_code}")
        if response.status_code == 200:
            print("✅ Queue endpoint works!")
            data = response.json()
            print(f"Response: {json.dumps(data, indent=2)}")
        else:
            print("❌ Queue endpoint failed")
            
        # Тест регистрации пользователя
        print("\nTesting user registration...")
        user_data = {
            "email": "localtest@example.com",
            "password": "testpass123",
            "nick_name": "LocalTestPlayer"
        }
        response = requests.post(
            f"{LOCAL_URL}/api-game-user/register",
            json=user_data,
            headers={'Content-Type': 'application/json'},
            timeout=5
        )
        print(f"Status: {response.status_code}")
        if response.status_code == 201:
            print("✅ User registration works!")
            data = response.json()
            print(f"Response: {json.dumps(data, indent=2)}")
            return data.get('player_id')
        else:
            print("❌ User registration failed")
            if response.status_code == 409:
                print("User already exists - that's ok!")
                return "user_1"  # Предполагаем что пользователь уже есть
            
    except requests.exceptions.ConnectionError:
        print("❌ Cannot connect to local server")
        print("Make sure the server is running on port 3329")
        return None
    except Exception as e:
        print(f"❌ Error: {e}")
        return None

def test_queue_with_user(player_id):
    """Тестирует очередь с реальным пользователем"""
    if not player_id:
        return
        
    print(f"\n🎮 TESTING QUEUE WITH USER: {player_id}")
    print("="*40)
    
    try:
        # Добавляем в очередь
        queue_data = {
            "queue_match_type": 0,  # 1v1
            "player_id": player_id
        }
        response = requests.post(
            f"{LOCAL_URL}/api-game-queue/",
            json=queue_data,
            headers={'Content-Type': 'application/json'},
            timeout=5
        )
        print(f"Add to queue status: {response.status_code}")
        if response.status_code == 200:
            print("✅ Added to queue successfully!")
            data = response.json()
            print(f"Response: {json.dumps(data, indent=2)}")
            
            # Проверяем статус в очереди
            time.sleep(1)
            response = requests.get(f"{LOCAL_URL}/api-game-queue/player/{player_id}", timeout=5)
            print(f"Queue status check: {response.status_code}")
            if response.status_code == 200:
                data = response.json()
                print(f"Queue status: {json.dumps(data, indent=2)}")
                
            # Удаляем из очереди
            remove_data = {"player_id": player_id}
            response = requests.delete(
                f"{LOCAL_URL}/api-game-queue/",
                json=remove_data,
                headers={'Content-Type': 'application/json'},
                timeout=5
            )
            print(f"Remove from queue status: {response.status_code}")
            if response.status_code == 200:
                print("✅ Removed from queue successfully!")
        else:
            print("❌ Failed to add to queue")
            
    except Exception as e:
        print(f"❌ Queue test error: {e}")

def main():
    """Основная функция"""
    print("🎯 LOCAL SERVER TEST")
    print("="*50)
    
    # Базовые тесты
    player_id = test_local_server()
    
    # Тест очереди с пользователем
    test_queue_with_user(player_id)
    
    print("\n🎉 LOCAL TESTING COMPLETED!")
    print("="*50)

if __name__ == "__main__":
    main() 