using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api-game-match")]
public class GameMatchController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly InMemoryMatchmakingService _memory;
    private readonly MatchmakingService _matchmaking;

    public GameMatchController(GameDbContext context, InMemoryMatchmakingService memory, MatchmakingService matchmaking)
    {
        _context = context;
        _memory = memory;
        _matchmaking = matchmaking;
    }

    // --- Только для истории! ---
    [HttpGet("user/{userId}/history")]
    public async Task<ActionResult> GetUserMatchHistory(int userId)
    {
        var matches = await _context.GameMatches
            .OrderByDescending(m => m.StartTime)
            .Take(100)
            .ToListAsync();
        var userMatches = matches.Where(m => m.PlayersList.Contains(userId)).Take(20).ToList();
        return Ok(userMatches);
    }

    // --- Только активные (in-memory) ---
    [HttpGet("user/{userId}")]
    public ActionResult GetUserMatches(int userId)
    {
        var activeMatches = _memory.GetUserActiveMatches(userId);
        return Ok(activeMatches);
    }

    [HttpGet("active")]
    public ActionResult GetActiveMatches()
    {
        var matches = _memory.GetAllActiveMatches();
        return Ok(matches);
    }

    [HttpGet("{matchId}")]
    public ActionResult GetMatch(int matchId)
    {
        if (_memory.TryGetActiveMatch(matchId, out var match))
            return Ok(match);
        var dbMatch = _context.GameMatches.Find(matchId);
        if (dbMatch != null)
            return Ok(dbMatch);
        return NotFound("Match not found");
    }

    [HttpGet("{matchId}/status")]
    public ActionResult GetMatchStatus(int matchId)
    {
        if (_memory.TryGetActiveMatch(matchId, out var match))
        {
            var elapsedTime = (DateTime.UtcNow - match.StartTime).TotalSeconds;
            return Ok(new
            {
                matchId = match.MatchId,
                status = match.Status,
                matchType = match.MatchType,
                elapsedTime = (int)elapsedTime,
                timeLimit = match.MatchMaxTimeLimit,
                isExpired = match.IsExpired,
                players = match.PlayersList,
                winners = match.WinnersList,
                losers = match.LosersList,
                draw = match.DrawList
            });
        }
        var dbMatch = _context.GameMatches.Find(matchId);
        if (dbMatch != null)
        {
            var elapsedTime = dbMatch.EndTime.HasValue 
                ? (dbMatch.EndTime.Value - dbMatch.StartTime).TotalSeconds 
                : (DateTime.UtcNow - dbMatch.StartTime).TotalSeconds;
            return Ok(new
            {
                matchId = dbMatch.MatchId,
                status = dbMatch.Status,
                matchType = dbMatch.MatchType,
                elapsedTime = (int)elapsedTime,
                timeLimit = dbMatch.MatchMaxTimeLimit,
                isExpired = dbMatch.IsExpired,
                players = dbMatch.PlayersList,
                winners = dbMatch.WinnersList,
                losers = dbMatch.LosersList,
                draw = dbMatch.DrawList
            });
        }
        return NotFound("Match not found");
    }

    // --- Завершение матчей ---
    [HttpPost("{matchId}/finish")]
    public async Task<ActionResult> FinishMatch(int matchId, [FromBody] FinishGameMatchRequest request)
    {
        // Завершаем матч через MatchmakingService (он сам сохранит в базу)
        await _matchmaking.FinishMatch(matchId, GameMatchStatus.Completed);
        return Ok(new { message = "Match finished successfully" });
    }

    [HttpPost("{matchId}/cancel")]
    public async Task<ActionResult> CancelMatch(int matchId, [FromBody] CancelGameMatchRequest request)
    {
        await _matchmaking.FinishMatch(matchId, GameMatchStatus.Cancelled);
        return Ok(new { message = "Match cancelled successfully" });
    }
}

public class FinishGameMatchRequest
{
    public List<int> Winners { get; set; } = new();
    public List<int> Losers { get; set; } = new();
}

public class CancelGameMatchRequest
{
    public string Reason { get; set; } = string.Empty;
} 