using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api-game-admin")]
public class AdminController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly string _adminToken = "admin123secure"; // В продакшене лучше использовать переменную окружения

    public AdminController(GameDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAdminPage()
    {
        var html = ReadFileContent("wwwroot/online-game/admin.html");
        return Content(html, "text/html");
    }

    [HttpPost("auth")]
    public IActionResult AuthenticateAdmin([FromBody] AdminAuthRequest request)
    {
        if (request.Token == _adminToken)
        {
            var adminSession = GenerateAdminSession();
            return Ok(new { success = true, sessionToken = adminSession });
        }
        return Unauthorized(new { success = false, message = "Неверный токен" });
    }

    [HttpPost("validate")]
    public IActionResult ValidateSession([FromBody] ValidateSessionRequest request)
    {
        // Простая проверка токена (в продакшене лучше использовать JWT)
        if (IsValidAdminSession(request.SessionToken))
        {
            return Ok(new { success = true });
        }
        return Unauthorized(new { success = false });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetAdminStatistics()
    {
        try
        {
            var stats = new AdminStatisticsDto
            {
                // Общая статистика
                TotalPlayers = await _context.Users.CountAsync(u => u.IsActive),
                OnlinePlayers = await _context.Users.CountAsync(u => u.IsActive && u.LastHeartbeat.HasValue && u.LastHeartbeat >= DateTime.UtcNow.AddMinutes(-2)),
                TotalMatches = await _context.GameMatches.CountAsync(m => m.Status == GameMatchStatus.Completed),
                ActiveMatches = await _context.GameMatches.CountAsync(m => m.Status == GameMatchStatus.InProgress),
                
                // Статистика по типам матчей
                OneVsOneMatches = await _context.GameMatches.CountAsync(m => m.MatchType == GameMatchType.OneVsOne),
                TwoVsTwoMatches = await _context.GameMatches.CountAsync(m => m.MatchType == GameMatchType.TwoVsTwo),
                FourPlayerFFAMatches = await _context.GameMatches.CountAsync(m => m.MatchType == GameMatchType.FourPlayerFFA),
                
                // Статистика за сегодня
                TodayNewPlayers = await _context.Users.CountAsync(u => u.CreatedAt.Date == DateTime.Today),
                TodayMatches = await _context.GameMatches.CountAsync(m => m.StartTime.Date == DateTime.Today),
                
                // Статистика за неделю
                WeekNewPlayers = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.Today.AddDays(-7)),
                WeekMatches = await _context.GameMatches.CountAsync(m => m.StartTime >= DateTime.Today.AddDays(-7))
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения статистики: {ex.Message}");
        }
    }

    [HttpGet("recent-matches")]
    public async Task<ActionResult> GetRecentMatches()
    {
        try
        {
            var matches = await _context.GameMatches
                .OrderByDescending(m => m.StartTime)
                .Take(10)
                .ToListAsync();

            var result = new List<object>();
            foreach (var match in matches)
            {
                // Получаем информацию об игроках
                var playerIds = match.PlayersList;
                var players = await _context.Users
                    .Where(u => playerIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Username })
                    .ToListAsync();

                result.Add(new
                {
                    MatchId = match.MatchId,
                    MatchType = match.MatchType.ToString(),
                    Status = match.Status.ToString(),
                    StartTime = match.StartTime,
                    EndTime = match.EndTime,
                    Duration = match.EndTime.HasValue ? (double?)(match.EndTime.Value - match.StartTime).TotalMinutes : null,
                    Players = players,
                    Winners = match.WinnersList.Any() ? await _context.Users.Where(u => match.WinnersList.Contains(u.Id)).Select(u => u.Username).ToListAsync() : null,
                    Losers = match.LosersList.Any() ? await _context.Users.Where(u => match.LosersList.Contains(u.Id)).Select(u => u.Username).ToListAsync() : null,
                    Draw = match.DrawList.Any()
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения последних матчей: {ex.Message}");
        }
    }

    [HttpGet("realtime-stats")]
    public async Task<ActionResult> GetRealtimeStats()
    {
        try
        {
            // Получаем in-memory статистику очередей
            var inMemoryService = HttpContext.RequestServices.GetRequiredService<InMemoryMatchmakingService>();
            
            var stats = new
            {
                // Очереди по типам матчей (in-memory)
                QueueStats = new
                {
                    OneVsOne = inMemoryService.GetQueue(GameMatchType.OneVsOne).Count,
                    TwoVsTwo = inMemoryService.GetQueue(GameMatchType.TwoVsTwo).Count,
                    FourPlayerFFA = inMemoryService.GetQueue(GameMatchType.FourPlayerFFA).Count,
                    Total = inMemoryService.GetQueue(GameMatchType.OneVsOne).Count + 
                           inMemoryService.GetQueue(GameMatchType.TwoVsTwo).Count + 
                           inMemoryService.GetQueue(GameMatchType.FourPlayerFFA).Count
                },
                
                // Активные матчи по типам (in-memory)
                ActiveMatches = new
                {
                    OneVsOne = inMemoryService.GetActiveMatches().Count(m => m.MatchType == GameMatchType.OneVsOne),
                    TwoVsTwo = inMemoryService.GetActiveMatches().Count(m => m.MatchType == GameMatchType.TwoVsTwo),
                    FourPlayerFFA = inMemoryService.GetActiveMatches().Count(m => m.MatchType == GameMatchType.FourPlayerFFA),
                    Total = inMemoryService.GetActiveMatches().Count
                },
                
                // Дополнительная информация
                ServerInfo = new
                {
                    OnlineNow = await _context.Users.CountAsync(u => u.IsActive && u.LastHeartbeat.HasValue && u.LastHeartbeat >= DateTime.UtcNow.AddMinutes(-2)),
                    LastUpdate = DateTime.UtcNow
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения real-time статистики: {ex.Message}");
        }
    }

    [HttpGet("top-players")]
    public async Task<ActionResult> GetTopPlayers()
    {
        try
        {
            var result = new
            {
                TopByScore = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderByDescending(u => u.Score)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Score,
                        u.GamesPlayed,
                        u.GamesWon,
                        WinRate = u.GamesPlayed > 0 ? (double)u.GamesWon / u.GamesPlayed * 100 : 0
                    })
                    .ToListAsync(),

                TopByOneVsOne = await _context.Users
                    .Where(u => u.IsActive && u.MmrOneVsOne > 0)
                    .OrderByDescending(u => u.MmrOneVsOne)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        MMR = u.MmrOneVsOne,
                        u.GamesPlayed,
                        u.GamesWon
                    })
                    .ToListAsync(),

                TopByTwoVsTwo = await _context.Users
                    .Where(u => u.IsActive && u.MmrTwoVsTwo > 0)
                    .OrderByDescending(u => u.MmrTwoVsTwo)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        MMR = u.MmrTwoVsTwo,
                        u.GamesPlayed,
                        u.GamesWon
                    })
                    .ToListAsync(),

                TopByFourPlayerFFA = await _context.Users
                    .Where(u => u.IsActive && u.MmrFourPlayerFFA > 0)
                    .OrderByDescending(u => u.MmrFourPlayerFFA)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        MMR = u.MmrFourPlayerFFA,
                        u.GamesPlayed,
                        u.GamesWon
                    })
                    .ToListAsync(),

                TopByGamesPlayed = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderByDescending(u => u.GamesPlayed)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.GamesPlayed,
                        u.GamesWon,
                        WinRate = u.GamesPlayed > 0 ? (double)u.GamesWon / u.GamesPlayed * 100 : 0
                    })
                    .ToListAsync()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения топ игроков: {ex.Message}");
        }
    }

    [HttpPost("clear-players")]
    public async Task<ActionResult> ClearPlayers([FromBody] ClearPlayersRequest request)
    {
        if (!IsValidAdminSession(request.SessionToken))
        {
            return Unauthorized(new { success = false, message = "Неверная сессия" });
        }

        try
        {
            var playersToDelete = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            if (request.OnlyInactive)
            {
                // Удаляем только неактивных игроков (не заходили более 30 дней)
                var threshold = DateTime.UtcNow.AddDays(-30);
                playersToDelete = playersToDelete.Where(u => u.LastLoginAt < threshold).ToList();
            }

            foreach (var player in playersToDelete)
            {
                player.IsActive = false; // Мягкое удаление
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, deletedCount = playersToDelete.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка очистки игроков: {ex.Message}");
        }
    }

    [HttpPost("clear-matches")]
    public async Task<ActionResult> ClearMatches([FromBody] ClearMatchesRequest request)
    {
        if (!IsValidAdminSession(request.SessionToken))
        {
            return Unauthorized(new { success = false, message = "Неверная сессия" });
        }

        try
        {
            var matchesToDelete = await _context.GameMatches.ToListAsync();

            if (request.OnlyCompleted)
            {
                matchesToDelete = matchesToDelete.Where(m => m.Status == GameMatchStatus.Completed).ToList();
            }

            if (request.OlderThanDays > 0)
            {
                var threshold = DateTime.UtcNow.AddDays(-request.OlderThanDays);
                matchesToDelete = matchesToDelete.Where(m => m.StartTime < threshold).ToList();
            }

            _context.GameMatches.RemoveRange(matchesToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, deletedCount = matchesToDelete.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка очистки матчей: {ex.Message}");
        }
    }

    [HttpPost("set-all-players-mmr")]
    public async Task<ActionResult> SetAllPlayersMMR([FromBody] SetAllPlayersMMRRequest request)
    {
        if (!IsValidAdminSession(request.SessionToken))
        {
            return Unauthorized(new { success = false, message = "Неверная сессия" });
        }

        try
        {
            var allUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            foreach (var user in allUsers)
            {
                user.MmrOneVsOne = request.MmrValue;
                user.MmrTwoVsTwo = request.MmrValue;
                user.MmrFourPlayerFFA = request.MmrValue;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                updatedCount = allUsers.Count,
                mmrValue = request.MmrValue,
                message = $"Установлен MMR {request.MmrValue} для {allUsers.Count} игроков по всем типам матчей" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при установке MMR: {ex.Message}");
        }
    }

    private string ReadFileContent(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            if (System.IO.File.Exists(fullPath))
            {
                return System.IO.File.ReadAllText(fullPath);
            }
            return "<!DOCTYPE html><html><head><title>Файл не найден</title></head><body><h1>Файл не найден</h1></body></html>";
        }
        catch (Exception ex)
        {
            return $"<!DOCTYPE html><html><head><title>Ошибка</title></head><body><h1>Ошибка загрузки файла: {ex.Message}</h1></body></html>";
        }
    }

    private string GenerateAdminSession()
    {
        // Генерируем простой токен сессии
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    private bool IsValidAdminSession(string sessionToken)
    {
        // Простая проверка (в продакшене лучше использовать JWT или Redis)
        return !string.IsNullOrEmpty(sessionToken) && sessionToken.Length > 20;
    }
}

public class AdminAuthRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ValidateSessionRequest
{
    public string SessionToken { get; set; } = string.Empty;
}

public class ClearPlayersRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public bool OnlyInactive { get; set; } = false;
}

public class ClearMatchesRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public bool OnlyCompleted { get; set; } = true;
    public int OlderThanDays { get; set; } = 0;
}

public class SetAllPlayersMMRRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public int MmrValue { get; set; } = 500;
}

public class AdminStatisticsDto
{
    public int TotalPlayers { get; set; }
    public int OnlinePlayers { get; set; }
    public int TotalMatches { get; set; }
    public int ActiveMatches { get; set; }
    public int OneVsOneMatches { get; set; }
    public int TwoVsTwoMatches { get; set; }
    public int FourPlayerFFAMatches { get; set; }
    public int TodayNewPlayers { get; set; }
    public int TodayMatches { get; set; }
    public int WeekNewPlayers { get; set; }
    public int WeekMatches { get; set; }
} 