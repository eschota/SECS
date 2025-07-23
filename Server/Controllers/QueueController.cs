using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

/// <summary>
/// Контроллер управления in-memory очередями матчмейкинга.
/// Все операции с очередями происходят только в памяти сервера.
/// </summary>
[ApiController]
[Route("api-game-queue")]
public class QueueController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly InMemoryMatchmakingService _memory;

    public QueueController(GameDbContext context, InMemoryMatchmakingService memory)
    {
        _context = context;
        _memory = memory;
    }

    /// <summary>
    /// Войти в очередь поиска (in-memory).
    /// </summary>
    [HttpPost("{userId}/join")]
    public async Task<ActionResult> JoinQueue(int userId, [FromBody] JoinQueueRequest request)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<QueueController>>();
        logger.LogInformation($"🎮 Player {userId} attempting to join queue for match type {request.MatchType}");
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // In-memory очередь
        _memory.RemoveFromQueue(userId, request.MatchType); // На всякий случай удаляем старую
        
        var queueEntry = new MatchQueue
        {
            UserId = userId,
            MatchType = request.MatchType,
            MmrRating = GetUserMmrForType(user, request.MatchType),
            JoinTime = DateTime.UtcNow
        };
        
        _memory.AddToQueue(queueEntry);
        
        logger.LogInformation($"✅ Player {userId} ({user.Username}) joined in-memory queue for {request.MatchType}");
        return Ok(new { message = "Successfully joined queue", queueType = request.MatchType });
    }

    /// <summary>
    /// Выйти из всех очередей поиска (in-memory).
    /// </summary>
    [HttpPost("{userId}/leave")]
    public async Task<ActionResult> LeaveQueue(int userId)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<QueueController>>();
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Удаляем из всех очередей (на всякий случай)
        foreach (GameMatchType type in Enum.GetValues(typeof(GameMatchType)))
            _memory.RemoveFromQueue(userId, type);
        
        logger.LogInformation($"✅ Player {userId} ({user.Username}) left all in-memory queues");
        return Ok(new { message = "Successfully left queue" });
    }

    /// <summary>
    /// Получить статус игрока в очереди (in-memory).
    /// </summary>
    [HttpGet("{userId}/status")]
    public async Task<ActionResult> GetQueueStatus(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Ищем игрока во всех очередях
        foreach (GameMatchType type in Enum.GetValues(typeof(GameMatchType)))
        {
            var queue = _memory.GetQueue(type).FirstOrDefault(q => q.UserId == userId);
            if (queue != null)
            {
                var queueTime = DateTime.UtcNow - queue.JoinTime;
                var currentThreshold = queue.CalculateCurrentMmrThreshold();
                return Ok(new
                {
                    inQueue = true,
                    queueType = queue.MatchType,
                    queueTime = (int)queueTime.TotalSeconds,
                    currentMmrThreshold = currentThreshold,
                    userMmr = queue.MmrRating
                });
            }
        }
        
        return Ok(new { inQueue = false });
    }

    /// <summary>
    /// Получить статистику по всем in-memory очередям.
    /// </summary>
    [HttpGet("stats")]
    public ActionResult GetQueueStats()
    {
        var oneVsOneCount = _memory.GetQueue(GameMatchType.OneVsOne).Count;
        var twoVsTwoCount = _memory.GetQueue(GameMatchType.TwoVsTwo).Count;
        var ffaCount = _memory.GetQueue(GameMatchType.FourPlayerFFA).Count;
        
        return Ok(new
        {
            oneVsOne = oneVsOneCount,
            twoVsTwo = twoVsTwoCount,
            fourPlayerFFA = ffaCount,
            total = oneVsOneCount + twoVsTwoCount + ffaCount
        });
    }

    private static int GetUserMmrForType(User user, GameMatchType matchType)
    {
        return matchType switch
        {
            GameMatchType.OneVsOne => user.MmrOneVsOne,
            GameMatchType.TwoVsTwo => user.MmrTwoVsTwo,
            GameMatchType.FourPlayerFFA => user.MmrFourPlayerFFA,
            _ => 0
        };
    }
}

public class JoinQueueRequest
{
    public GameMatchType MatchType { get; set; }
} 