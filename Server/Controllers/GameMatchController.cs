using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api-game-match")]
public class GameMatchController : ControllerBase
{
    private readonly GameDbContext _context;

    public GameMatchController(GameDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public async Task<ActionResult<GameMatch>> CreateMatch([FromBody] CreateGameMatchRequest request)
    {
        // Проверяем, что все игроки существуют
        var players = await _context.Users
            .Where(u => request.PlayerIds.Contains(u.Id))
            .ToListAsync();

        if (players.Count != request.PlayerIds.Count)
            return BadRequest("Some players not found");

        // Проверяем, что никто не в матче
        if (players.Any(p => p.CurrentMatchId != null))
            return BadRequest("Some players are already in a match");

        var match = new GameMatch
        {
            MatchType = request.MatchType,
            PlayersList = request.PlayerIds,
            TeamsList = request.TeamIds,
            StartTime = DateTime.UtcNow,
            Status = GameMatchStatus.InProgress
        };

        _context.GameMatches.Add(match);

        // Обновляем статус игроков
        foreach (var player in players)
        {
            player.CurrentMatchId = match.MatchId;
            player.IsInQueue = false;
        }

        // Удаляем игроков из очереди
        var queueEntries = await _context.MatchQueues
            .Where(q => request.PlayerIds.Contains(q.UserId))
            .ToListAsync();

        _context.MatchQueues.RemoveRange(queueEntries);

        await _context.SaveChangesAsync();

        return Ok(match);
    }

    [HttpPost("{matchId}/finish")]
    public async Task<ActionResult> FinishMatch(int matchId, [FromBody] FinishGameMatchRequest request)
    {
        var match = await _context.GameMatches.FindAsync(matchId);
        if (match == null)
            return NotFound("Match not found");

        if (match.Status != GameMatchStatus.InProgress)
            return BadRequest("Match is not in progress");

        match.Status = GameMatchStatus.Completed;
        match.EndTime = DateTime.UtcNow;

        // Обрабатываем результаты
        if (request.Winners.Any())
        {
            match.WinnersList = request.Winners;
            match.LosersList = request.Losers;
        }
        else
        {
            match.DrawList = match.PlayersList;
        }

        // Обновляем MMR игроков
        await UpdatePlayerMmr(match, request.Winners, request.Losers);

        // Освобождаем игроков от матча
        var players = await _context.Users
            .Where(u => match.PlayersList.Contains(u.Id))
            .ToListAsync();

        foreach (var player in players)
        {
            player.CurrentMatchId = null;
            player.GamesPlayed++;
            if (request.Winners.Contains(player.Id))
                player.GamesWon++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Match finished successfully" });
    }

    [HttpPost("{matchId}/cancel")]
    public async Task<ActionResult> CancelMatch(int matchId, [FromBody] CancelGameMatchRequest request)
    {
        var match = await _context.GameMatches.FindAsync(matchId);
        if (match == null)
            return NotFound("Match not found");

        if (match.Status != GameMatchStatus.InProgress)
            return BadRequest("Match is not in progress");

        match.Status = GameMatchStatus.Cancelled;
        match.EndTime = DateTime.UtcNow;

        // Освобождаем игроков от матча
        var players = await _context.Users
            .Where(u => match.PlayersList.Contains(u.Id))
            .ToListAsync();

        foreach (var player in players)
        {
            player.CurrentMatchId = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Match cancelled successfully" });
    }

    [HttpGet("{matchId}")]
    public async Task<ActionResult<GameMatch>> GetMatch(int matchId)
    {
        var match = await _context.GameMatches.FindAsync(matchId);
        if (match == null)
            return NotFound("Match not found");

        return Ok(match);
    }

    [HttpGet("{matchId}/status")]
    public async Task<ActionResult> GetMatchStatus(int matchId)
    {
        var match = await _context.GameMatches.FindAsync(matchId);
        if (match == null)
            return NotFound("Match not found");

        var elapsedTime = match.EndTime.HasValue 
            ? (match.EndTime.Value - match.StartTime).TotalSeconds 
            : (DateTime.UtcNow - match.StartTime).TotalSeconds;

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

    [HttpGet("active")]
    public async Task<ActionResult> GetActiveMatches()
    {
        var matches = await _context.GameMatches
            .Where(m => m.Status == GameMatchStatus.InProgress)
            .OrderBy(m => m.StartTime)
            .ToListAsync();

        return Ok(matches);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult> GetUserMatches(int userId)
    {
        var matches = await _context.GameMatches
            .OrderByDescending(m => m.StartTime)
            .Take(100)
            .ToListAsync();

        // Фильтруем по пользователю в памяти
        var userMatches = matches.Where(m => m.PlayersList.Contains(userId)).Take(20).ToList();

        return Ok(userMatches);
    }

    [HttpPost("check-timeouts")]
    public async Task<ActionResult> CheckTimeouts()
    {
        var expiredMatches = await _context.GameMatches
            .Where(m => m.Status == GameMatchStatus.InProgress)
            .ToListAsync();
        
        // Фильтруем истекшие матчи в памяти
        expiredMatches = expiredMatches.Where(m => m.IsExpired).ToList();

        foreach (var match in expiredMatches)
        {
            match.Status = GameMatchStatus.Completed;
            match.EndTime = DateTime.UtcNow;

            // Выбираем случайного победителя
            var random = new Random();
            var winnerIndex = random.Next(match.PlayersList.Count);
            var winnerId = match.PlayersList[winnerIndex];

            match.WinnersList = new List<int> { winnerId };
            match.LosersList = match.PlayersList.Where(id => id != winnerId).ToList();

            // MMR обновляется автоматически в MatchmakingService при timeout
            // Освобождаем игроков (статистики обновляются в MatchmakingService)
            var players = await _context.Users
                .Where(u => match.PlayersList.Contains(u.Id))
                .ToListAsync();

            foreach (var player in players)
            {
                player.CurrentMatchId = null;
                // GamesPlayed и GamesWon обновляются в MatchmakingService
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { expiredMatches = expiredMatches.Count });
    }

    private async Task UpdatePlayerMmr(GameMatch match, List<int> winners, List<int> losers)
    {
        var players = await _context.Users
            .Where(u => match.PlayersList.Contains(u.Id))
            .ToListAsync();

        foreach (var player in players)
        {
            int mmrChange = 0;

            if (winners.Contains(player.Id))
            {
                mmrChange = +20; // Победа
            }
            else if (losers.Contains(player.Id))
            {
                mmrChange = -20; // Поражение
            }
            else
            {
                mmrChange = +5; // Ничья
            }

            // Обновляем MMR для соответствующего типа матча
            switch (match.MatchType)
            {
                case GameMatchType.OneVsOne:
                    player.MmrOneVsOne = Math.Max(500, player.MmrOneVsOne + mmrChange);
                    break;
                case GameMatchType.TwoVsTwo:
                    player.MmrTwoVsTwo = Math.Max(500, player.MmrTwoVsTwo + mmrChange);
                    break;
                case GameMatchType.FourPlayerFFA:
                    player.MmrFourPlayerFFA = Math.Max(500, player.MmrFourPlayerFFA + mmrChange);
                    break;
            }
        }
    }
}

public class CreateGameMatchRequest
{
    public GameMatchType MatchType { get; set; }
    public List<int> PlayerIds { get; set; } = new();
    public List<int> TeamIds { get; set; } = new();
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