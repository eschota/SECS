#!/usr/bin/env powershell

# Утилита для очистки базы данных от застрявших игроков
Write-Host "🔧 Cleaning up database from stuck players..." -ForegroundColor Yellow

# Подключаемся к базе данных
$dbPath = "game.db"

if (Test-Path $dbPath) {
    Write-Host "✅ Database found: $dbPath" -ForegroundColor Green
    
    # Очищаем игроков с CurrentMatchId = 0
    Write-Host "🧹 Clearing players with CurrentMatchId = 0..." -ForegroundColor Cyan
    sqlite3 $dbPath "UPDATE Users SET CurrentMatchId = NULL WHERE CurrentMatchId = 0 OR CurrentMatchId IS NULL;"
    
    # Сбрасываем статус очереди для всех игроков
    Write-Host "🔄 Resetting queue status for all players..." -ForegroundColor Cyan
    sqlite3 $dbPath "UPDATE Users SET IsInQueue = 0;"
    
    # Очищаем очередь
    Write-Host "🗑️ Clearing match queue..." -ForegroundColor Cyan
    sqlite3 $dbPath "DELETE FROM MatchQueues;"
    
    # Показываем результат
    Write-Host "📊 Database cleanup results:" -ForegroundColor Green
    
    $playersInMatch = sqlite3 $dbPath "SELECT COUNT(*) FROM Users WHERE CurrentMatchId IS NOT NULL;"
    $playersInQueue = sqlite3 $dbPath "SELECT COUNT(*) FROM Users WHERE IsInQueue = 1;"
    $queueEntries = sqlite3 $dbPath "SELECT COUNT(*) FROM MatchQueues;"
    
    Write-Host "   Players in match: $playersInMatch" -ForegroundColor White
    Write-Host "   Players in queue: $playersInQueue" -ForegroundColor White  
    Write-Host "   Queue entries: $queueEntries" -ForegroundColor White
    
    Write-Host "✅ Database cleanup completed!" -ForegroundColor Green
} else {
    Write-Host "❌ Database not found: $dbPath" -ForegroundColor Red
    exit 1
} 