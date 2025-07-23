#!/usr/bin/env python3

import requests
import time

def test_server():
    print("🔍 Testing Server Endpoints...")
    
    base_url = "https://renderfin.com"
    
    # Test 1: Main server
    print("\n1️⃣ Testing main server...")
    try:
        response = requests.get(f"{base_url}/", timeout=10)
        print(f"   Status: {response.status_code}")
        print(f"   Response: {response.text[:100]}...")
    except Exception as e:
        print(f"   ❌ Error: {e}")
    
    # Test 2: Queue endpoint
    print("\n2️⃣ Testing queue endpoint...")
    try:
        response = requests.get(f"{base_url}/api-game-queue/", timeout=10)
        print(f"   Status: {response.status_code}")
        print(f"   Response: {response.text[:200]}...")
    except Exception as e:
        print(f"   ❌ Error: {e}")
    
    # Test 3: Player status endpoint
    print("\n3️⃣ Testing player status endpoint...")
    try:
        response = requests.get(f"{base_url}/api-game-queue/player/test123", timeout=10)
        print(f"   Status: {response.status_code}")
        print(f"   Response: {response.text}")
    except Exception as e:
        print(f"   ❌ Error: {e}")
    
    # Test 4: Statistics endpoint
    print("\n4️⃣ Testing statistics endpoint...")
    try:
        response = requests.get(f"{base_url}/api-game-statistics/", timeout=10)
        print(f"   Status: {response.status_code}")
        print(f"   Response: {response.text}")
    except Exception as e:
        print(f"   ❌ Error: {e}")

if __name__ == "__main__":
    test_server() 