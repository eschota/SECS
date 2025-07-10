import requests
import json

try:
    print("🔍 QUICK STATS CHECK")
    print("="*40)
    
    # Проверяем лобби
    r = requests.get('http://localhost:3329/api-game-lobby/', timeout=5)
    if r.status_code == 200:
        data = r.json()
        stats = data.get('stats', {})
        print(f"👥 Total users: {stats.get('total_users_count', 0)}")
        print(f"🏛️ Lobby users: {stats.get('lobby_users_count', 0)}")
        print(f"⏱️ Queue users: {stats.get('queue_users_count', 0)}")
        print(f"🎮 Active matches: {stats.get('active_matches_count', 0)}")
    
    # Проверяем очереди
    r = requests.get('http://localhost:3329/api-game-queue/', timeout=5)
    if r.status_code == 200:
        data = r.json()
        queues = data.get('queues', {})
        print("\n📊 QUEUES:")
        for queue_id, queue_info in queues.items():
            match_type = queue_info.get('match_type')
            current = queue_info.get('current_players', 0)
            required = queue_info.get('players_required', 0)
            print(f"  {match_type}: {current}/{required}")
    
    # Проверяем матчи
    r = requests.get('http://localhost:3329/api-game-match/', timeout=5)
    if r.status_code == 200:
        data = r.json()
        active = len(data.get('active_matches', []))
        history = len(data.get('history_matches', []))
        print(f"\n🎮 MATCHES: Active={active}, History={history}")
        
    print("\n✅ Stats check completed!")
    
except Exception as e:
    print(f"❌ Error: {e}") 