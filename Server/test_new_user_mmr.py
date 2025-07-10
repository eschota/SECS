#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import requests
import json
import time
import random
import string

def generate_random_email():
    """Генерирует случайный email"""
    random_string = ''.join(random.choices(string.ascii_lowercase + string.digits, k=10))
    return f"test_{random_string}@example.com"

def generate_random_username():
    """Генерирует случайное имя пользователя"""
    random_string = ''.join(random.choices(string.ascii_lowercase + string.digits, k=8))
    return f"TestUser_{random_string}"

def test_new_user_registration():
    """Тестирует регистрацию нового пользователя и проверяет MMR"""
    
    # Данные для регистрации
    email = generate_random_email()
    username = generate_random_username()
    password = "test123456"
    
    register_data = {
        "username": username,
        "email": email,
        "password": password,
        "avatar": "https://www.gravatar.com/avatar/?d=mp"
    }
    
    print(f"🔄 Регистрация нового пользователя...")
    print(f"📧 Email: {email}")
    print(f"👤 Username: {username}")
    
    try:
        # Регистрация
        response = requests.post(
            "https://renderfin.com/api-game-player",
            json=register_data,
            timeout=10
        )
        
        if response.status_code == 201:
            user_data = response.json()
            print(f"✅ Успешная регистрация!")
            print(f"🆔 User ID: {user_data['id']}")
            print(f"📊 MMR 1v1: {user_data['mmrOneVsOne']}")
            print(f"📊 MMR 2v2: {user_data['mmrTwoVsTwo']}")
            print(f"📊 MMR FFA: {user_data['mmrFourPlayerFFA']}")
            
            # Проверка MMR значений
            expected_mmr = 500
            errors = []
            
            if user_data['mmrOneVsOne'] != expected_mmr:
                errors.append(f"MMR 1v1: ожидалось {expected_mmr}, получено {user_data['mmrOneVsOne']}")
            
            if user_data['mmrTwoVsTwo'] != expected_mmr:
                errors.append(f"MMR 2v2: ожидалось {expected_mmr}, получено {user_data['mmrTwoVsTwo']}")
            
            if user_data['mmrFourPlayerFFA'] != expected_mmr:
                errors.append(f"MMR FFA: ожидалось {expected_mmr}, получено {user_data['mmrFourPlayerFFA']}")
            
            if errors:
                print(f"❌ Ошибки MMR:")
                for error in errors:
                    print(f"   - {error}")
                return False
            else:
                print(f"✅ Все MMR значения корректны (500)!")
                return True
        
        else:
            print(f"❌ Ошибка регистрации: {response.status_code}")
            print(f"📝 Ответ: {response.text}")
            return False
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Ошибка сети: {e}")
        return False

def test_login():
    """Тестирует логин существующего пользователя"""
    
    # Попробуем логин с данными из базы
    login_data = {
        "email": "test@example.com",
        "password": "password123"
    }
    
    print(f"\n🔄 Проверка логина существующего пользователя...")
    
    try:
        response = requests.post(
            "https://renderfin.com/api-game-player/login",
            json=login_data,
            timeout=10
        )
        
        if response.status_code == 200:
            user_data = response.json()
            print(f"✅ Успешный логин!")
            print(f"🆔 User ID: {user_data['id']}")
            print(f"👤 Username: {user_data['username']}")
            print(f"📊 MMR 1v1: {user_data['mmrOneVsOne']}")
            print(f"📊 MMR 2v2: {user_data['mmrTwoVsTwo']}")
            print(f"📊 MMR FFA: {user_data['mmrFourPlayerFFA']}")
            return True
        else:
            print(f"❌ Ошибка логина: {response.status_code}")
            print(f"📝 Ответ: {response.text}")
            return False
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Ошибка сети: {e}")
        return False

if __name__ == "__main__":
    print("🔧 Тестирование MMR для новых пользователей")
    print("=" * 50)
    
    # Ждем чтобы сервер запустился
    time.sleep(3)
    
    # Тестируем регистрацию нового пользователя
    success = test_new_user_registration()
    
    if success:
        print("\n✅ Тест пройден! Новые пользователи получают 500 MMR!")
    else:
        print("\n❌ Тест не пройден! Проверьте конфигурацию MMR.")
    
    # Тестируем логин
    test_login()
    
    print("\n🔚 Тестирование завершено.") 