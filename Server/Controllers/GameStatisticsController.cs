using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api-game-statistics")]
public class GameStatisticsController : ControllerBase
{
    private readonly GameDbContext _context;

    public GameStatisticsController(GameDbContext context)
    {
        _context = context;
    }

    // GET: api-game-statistics
    [HttpGet]
    public async Task<ActionResult<GameStatisticsDto>> GetGameStatistics()
    {
        try
        {
            // Общее количество зарегистрированных игроков
            var totalPlayers = await _context.Users
                .Where(u => u.IsActive)
                .CountAsync();

            // Количество игроков онлайн (на основе heartbeat - за последние 2 минуты)
            var onlineThreshold = DateTime.UtcNow.AddMinutes(-2);
            var onlinePlayers = await _context.Users
                .Where(u => u.IsActive && u.LastHeartbeat.HasValue && u.LastHeartbeat >= onlineThreshold)
                .CountAsync();

            // Общее количество проведенных матчей
            var totalMatches = await _context.GameMatches
                .Where(m => m.Status == GameMatchStatus.Completed)
                .CountAsync();

            // Количество активных матчей (в реальном времени)
            var liveMatches = await _context.GameMatches
                .Where(m => m.Status == GameMatchStatus.InProgress)
                .CountAsync();

            var statistics = new GameStatisticsDto
            {
                TotalPlayers = totalPlayers,
                OnlinePlayers = onlinePlayers,
                TotalMatches = totalMatches,
                LiveMatches = liveMatches
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения статистики: {ex.Message}");
        }
    }
}

public class GameStatisticsDto
{
    public int TotalPlayers { get; set; }
    public int OnlinePlayers { get; set; }
    public int TotalMatches { get; set; }
    public int LiveMatches { get; set; }
} 