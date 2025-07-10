using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api-game-queue")]
public class QueueController : ControllerBase
{
    private readonly GameDbContext _context;

    public QueueController(GameDbContext context)
    {
        _context = context;
    }

    [HttpPost("{userId}/join")]
    public async Task<ActionResult> JoinQueue(int userId, [FromBody] JoinQueueRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        if (user.IsInQueue)
            return BadRequest("User is already in queue");

        if (user.CurrentMatchId != null)
            return BadRequest("User is already in a match");

        // Проверяем, существует ли уже запись в очереди
        var existingQueue = await _context.MatchQueues
            .FirstOrDefaultAsync(q => q.UserId == userId);
        
        if (existingQueue != null)
        {
            // Обновляем существующую запись
            existingQueue.MatchType = request.MatchType;
            existingQueue.MmrRating = GetUserMmrForType(user, request.MatchType);
            existingQueue.JoinTime = DateTime.UtcNow;
        }
        else
        {
            // Создаем новую запись
            var queueEntry = new MatchQueue
            {
                UserId = userId,
                MatchType = request.MatchType,
                MmrRating = GetUserMmrForType(user, request.MatchType),
                JoinTime = DateTime.UtcNow
            };

            _context.MatchQueues.Add(queueEntry);
        }

        user.IsInQueue = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully joined queue", queueType = request.MatchType });
    }

    [HttpPost("{userId}/leave")]
    public async Task<ActionResult> LeaveQueue(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var queueEntry = await _context.MatchQueues
            .FirstOrDefaultAsync(q => q.UserId == userId);

        if (queueEntry == null)
            return BadRequest("User is not in queue");

        _context.MatchQueues.Remove(queueEntry);
        user.IsInQueue = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully left queue" });
    }

    [HttpGet("{userId}/status")]
    public async Task<ActionResult> GetQueueStatus(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var queueEntry = await _context.MatchQueues
            .FirstOrDefaultAsync(q => q.UserId == userId);

        if (queueEntry == null)
            return Ok(new { inQueue = false });

        var queueTime = DateTime.UtcNow - queueEntry.JoinTime;
        var currentThreshold = queueEntry.CalculateCurrentMmrThreshold();

        return Ok(new
        {
            inQueue = true,
            queueType = queueEntry.MatchType,
            queueTime = (int)queueTime.TotalSeconds,
            currentMmrThreshold = currentThreshold,
            userMmr = queueEntry.MmrRating
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetQueueStats()
    {
        var oneVsOneCount = await _context.MatchQueues
            .CountAsync(q => q.MatchType == GameMatchType.OneVsOne);

        var twoVsTwoCount = await _context.MatchQueues
            .CountAsync(q => q.MatchType == GameMatchType.TwoVsTwo);

        var ffaCount = await _context.MatchQueues
            .CountAsync(q => q.MatchType == GameMatchType.FourPlayerFFA);

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