import requests
import json
import time
import uuid

# API URLs
BASE_URL = "https://renderfin.com"
PLAYER_URL = f"{BASE_URL}/api-game-player/"
QUEUE_URL = f"{BASE_URL}/api-game-queue/"
MATCH_URL = f"{BASE_URL}/api-game-match/"
STATS_URL = f"{BASE_URL}/api-game-statistics/"

def test_player_registration():
    """Test player registration API"""
    print("=== Testing Player Registration ===")
    
    # Create developer user like Unity does
    dev_guid = str(uuid.uuid4())
    dev_username = f"Developer account {dev_guid}"
    dev_email = f"dev_{dev_guid}@local.dev"
    dev_password = "dev123"
    
    registration_data = {
        "username": dev_username,
        "email": dev_email,
        "password": dev_password,
        "avatar": "https://example.com/default-avatar.png"
    }
    
    try:
        response = requests.post(PLAYER_URL, json=registration_data, verify=False)
        print(f"Registration Status: {response.status_code}")
        print(f"Response: {response.text}")
        
        if response.status_code == 201:
            user_data = response.json()
            print(f"Successfully registered user: {user_data['username']} (ID: {user_data['id']})")
            return user_data
        else:
            print(f"Registration failed: {response.text}")
            return None
            
    except Exception as e:
        print(f"Registration error: {e}")
        return None

def test_heartbeat(user_id):
    """Test heartbeat API"""
    print(f"\n=== Testing Heartbeat for user {user_id} ===")
    
    heartbeat_data = {
        "userId": user_id,
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    }
    
    try:
        response = requests.post(f"{PLAYER_URL}heartbeat", json=heartbeat_data, verify=False)
        print(f"Heartbeat Status: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
        
    except Exception as e:
        print(f"Heartbeat error: {e}")
        return False

def test_queue_join(user_id):
    """Test queue join API"""
    print(f"\n=== Testing Queue Join for user {user_id} ===")
    
    queue_data = {
        "matchType": 1  # 1v1
    }
    
    try:
        response = requests.post(f"{QUEUE_URL}{user_id}/join", json=queue_data, verify=False)
        print(f"Queue Join Status: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
        
    except Exception as e:
        print(f"Queue join error: {e}")
        return False

def test_queue_status(user_id):
    """Test queue status API"""
    print(f"\n=== Testing Queue Status for user {user_id} ===")
    
    try:
        response = requests.get(f"{QUEUE_URL}{user_id}/status", verify=False)
        print(f"Queue Status Status: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
        
    except Exception as e:
        print(f"Queue status error: {e}")
        return False

def test_queue_leave(user_id):
    """Test queue leave API"""
    print(f"\n=== Testing Queue Leave for user {user_id} ===")
    
    try:
        response = requests.post(f"{QUEUE_URL}{user_id}/leave", verify=False)
        print(f"Queue Leave Status: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
        
    except Exception as e:
        print(f"Queue leave error: {e}")
        return False

def test_server_stats():
    """Test server statistics API"""
    print(f"\n=== Testing Server Statistics ===")
    
    try:
        response = requests.get(STATS_URL, verify=False)
        print(f"Stats Status: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
        
    except Exception as e:
        print(f"Stats error: {e}")
        return False

def main():
    print("🎮 Unity API Testing Script")
    print("=" * 40)
    
    # Test server availability
    print("Testing server availability...")
    if not test_server_stats():
        print("❌ Server is not available!")
        return
    
    print("✅ Server is available!")
    
    # Test player registration
    user_data = test_player_registration()
    if not user_data:
        print("❌ Player registration failed!")
        return
    
    user_id = user_data['id']
    print(f"✅ Player registered successfully! User ID: {user_id}")
    
    # Test heartbeat
    if test_heartbeat(user_id):
        print("✅ Heartbeat working!")
    else:
        print("❌ Heartbeat failed!")
    
    # Test queue operations
    if test_queue_join(user_id):
        print("✅ Queue join working!")
        
        # Wait a bit and check status
        time.sleep(2)
        if test_queue_status(user_id):
            print("✅ Queue status working!")
        
        # Leave queue
        if test_queue_leave(user_id):
            print("✅ Queue leave working!")
    else:
        print("❌ Queue operations failed!")
    
    print("\n" + "=" * 40)
    print("🎯 Testing complete!")

if __name__ == "__main__":
    main() 