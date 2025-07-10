using Microsoft.AspNetCore.Mvc;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api-game-match")]
public class MatchController : ControllerBase
{
    private static readonly Dictionary<string, Match> Matches = new();
    private static readonly object MatchLock = new();

    [HttpGet]
    public ActionResult<IEnumerable<Match>> GetMatches()
    {
        lock (MatchLock)
        {
            return Ok(Matches.Values.OrderByDescending(m => m.StartedAt));
        }
    }

    [HttpGet("{matchId}")]
    public ActionResult<Match> GetMatch(string matchId)
    {
        lock (MatchLock)
        {
            if (Matches.TryGetValue(matchId, out var match))
                return Ok(match);
            
            return NotFound();
        }
    }

    [HttpPost]
    public ActionResult<Match> CreateMatch([FromBody] CreateMatchRequest request)
    {
        lock (MatchLock)
        {
            var match = new Match
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Players = request.Players.Select(p => new GamePlayer
                {
                    Id = p.PlayerId,
                    Name = p.PlayerName,
                    JoinedAt = DateTime.UtcNow,
                    IsReady = false,
                    Score = 0
                }).ToList(),
                StartedAt = DateTime.UtcNow,
                Status = MatchStatus.Starting,
                GameData = new Dictionary<string, object>()
            };

            Matches[match.Id] = match;
            return CreatedAtAction(nameof(GetMatch), new { matchId = match.Id }, match);
        }
    }

    [HttpPost("{matchId}/start")]
    public ActionResult StartMatch(string matchId)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            if (match.Status != MatchStatus.Starting)
                return BadRequest("Match cannot be started");

            match.Status = MatchStatus.InProgress;
            match.StartedAt = DateTime.UtcNow;

            return Ok(match);
        }
    }

    [HttpPost("{matchId}/finish")]
    public ActionResult FinishMatch(string matchId, [FromBody] FinishMatchRequest request)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            if (match.Status != MatchStatus.InProgress)
                return BadRequest("Match is not in progress");

            match.Status = MatchStatus.Finished;
            match.FinishedAt = DateTime.UtcNow;
            match.WinnerId = request.WinnerId;
            match.Duration = (int)(match.FinishedAt.Value - match.StartedAt).TotalSeconds;

            // Обновляем счета игроков
            foreach (var scoreUpdate in request.PlayerScores)
            {
                var player = match.Players.FirstOrDefault(p => p.Id == scoreUpdate.PlayerId);
                if (player != null)
                {
                    player.Score = scoreUpdate.Score;
                }
            }

            return Ok(match);
        }
    }

    [HttpPost("{matchId}/cancel")]
    public ActionResult CancelMatch(string matchId, [FromBody] CancelMatchRequest request)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            if (match.Status == MatchStatus.Finished)
                return BadRequest("Cannot cancel finished match");

            match.Status = MatchStatus.Cancelled;
            match.FinishedAt = DateTime.UtcNow;

            return Ok(match);
        }
    }

    [HttpPost("{matchId}/player/{playerId}/ready")]
    public ActionResult SetPlayerReady(string matchId, string playerId, [FromBody] MatchPlayerReadyRequest request)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            var player = match.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return NotFound("Player not found in match");

            player.IsReady = request.IsReady;

            // Если все игроки готовы, можно автоматически начать матч
            if (match.Status == MatchStatus.Starting && match.Players.All(p => p.IsReady))
            {
                match.Status = MatchStatus.InProgress;
                match.StartedAt = DateTime.UtcNow;
            }

            return Ok(match);
        }
    }

    [HttpPost("{matchId}/update-score")]
    public ActionResult UpdateScore(string matchId, [FromBody] UpdateScoreRequest request)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            if (match.Status != MatchStatus.InProgress)
                return BadRequest("Match is not in progress");

            var player = match.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found in match");

            player.Score = request.Score;
            return Ok(match);
        }
    }

    [HttpGet("{matchId}/status")]
    public ActionResult<MatchStatusResponse> GetMatchStatus(string matchId)
    {
        lock (MatchLock)
        {
            if (!Matches.TryGetValue(matchId, out var match))
                return NotFound();

            var status = new MatchStatusResponse
            {
                MatchId = match.Id,
                Status = match.Status,
                ElapsedTime = match.Status == MatchStatus.InProgress 
                    ? (int)(DateTime.UtcNow - match.StartedAt).TotalSeconds 
                    : match.Duration,
                Players = match.Players.Select(p => new PlayerStatusDto
                {
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    Score = p.Score,
                    IsReady = p.IsReady
                }).ToList()
            };

            return Ok(status);
        }
    }

    [HttpDelete("{matchId}")]
    public ActionResult DeleteMatch(string matchId)
    {
        lock (MatchLock)
        {
            if (!Matches.ContainsKey(matchId))
                return NotFound();

            Matches.Remove(matchId);
            return NoContent();
        }
    }
}

public class CreateMatchRequest
{
    public string Name { get; set; } = string.Empty;
    public List<PlayerRequestDto> Players { get; set; } = new();
}



public class FinishMatchRequest
{
    public string? WinnerId { get; set; }
    public List<PlayerScoreDto> PlayerScores { get; set; } = new();
}

public class PlayerScoreDto
{
    public string PlayerId { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class CancelMatchRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class MatchPlayerReadyRequest
{
    public bool IsReady { get; set; }
}



public class UpdateScoreRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class MatchStatusResponse
{
    public string MatchId { get; set; } = string.Empty;
    public MatchStatus Status { get; set; }
    public int ElapsedTime { get; set; }
    public List<PlayerStatusDto> Players { get; set; } = new();
}

 