using Microsoft.AspNetCore.Mvc;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api-game-lobby")]
public class LobbyController : ControllerBase
{
    private static readonly Dictionary<string, Lobby> Lobbies = new();
    private static readonly object LobbyLock = new();

    [HttpGet]
    public ActionResult<IEnumerable<Lobby>> GetLobbies()
    {
        lock (LobbyLock)
        {
            return Ok(Lobbies.Values.Where(l => l.IsPublic).OrderByDescending(l => l.CreatedAt));
        }
    }

    [HttpGet("{lobbyId}")]
    public ActionResult<Lobby> GetLobby(string lobbyId)
    {
        lock (LobbyLock)
        {
            if (Lobbies.TryGetValue(lobbyId, out var lobby))
                return Ok(lobby);
            
            return NotFound();
        }
    }

    [HttpPost]
    public ActionResult<Lobby> CreateLobby([FromBody] CreateLobbyRequest request)
    {
        lock (LobbyLock)
        {
            var lobby = new Lobby
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                CreatorId = request.CreatorId,
                MaxPlayers = request.MaxPlayers,
                CreatedAt = DateTime.UtcNow,
                Status = LobbyStatus.Waiting,
                IsPublic = request.IsPublic,
                Password = request.Password,
                Players = new List<GamePlayer>()
            };

            // Добавляем создателя в лобби
            lobby.Players.Add(new GamePlayer
            {
                Id = request.CreatorId,
                Name = request.CreatorName,
                JoinedAt = DateTime.UtcNow,
                IsReady = false,
                Score = 0
            });

            Lobbies[lobby.Id] = lobby;
            return CreatedAtAction(nameof(GetLobby), new { lobbyId = lobby.Id }, lobby);
        }
    }

    [HttpPost("{lobbyId}/join")]
    public ActionResult JoinLobby(string lobbyId, [FromBody] JoinLobbyRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            if (lobby.Status != LobbyStatus.Waiting)
                return BadRequest("Lobby is not accepting players");

            if (lobby.Players.Count >= lobby.MaxPlayers)
                return BadRequest("Lobby is full");

            if (lobby.Players.Any(p => p.Id == request.PlayerId))
                return BadRequest("Player already in lobby");

            if (lobby.Players.Any(p => p.Name == request.PlayerName))
                return BadRequest("Player name already taken");

            // Проверка пароля для приватных лобби
            if (!lobby.IsPublic && lobby.Password != request.Password)
                return BadRequest("Invalid password");

            var player = new GamePlayer
            {
                Id = request.PlayerId,
                Name = request.PlayerName,
                JoinedAt = DateTime.UtcNow,
                IsReady = false,
                Score = 0
            };

            lobby.Players.Add(player);
            return Ok(lobby);
        }
    }

    [HttpPost("{lobbyId}/leave")]
    public ActionResult LeaveLobby(string lobbyId, [FromBody] LeaveLobbyRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            var player = lobby.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found in lobby");

            lobby.Players.Remove(player);

            // Если создатель покинул лобби, удаляем его или назначаем нового создателя
            if (lobby.CreatorId == request.PlayerId)
            {
                if (lobby.Players.Count > 0)
                {
                    lobby.CreatorId = lobby.Players.First().Id;
                }
                else
                {
                    Lobbies.Remove(lobbyId);
                    return Ok();
                }
            }

            return Ok(lobby);
        }
    }

    [HttpPost("{lobbyId}/ready")]
    public ActionResult SetPlayerReady(string lobbyId, [FromBody] SetPlayerReadyRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            var player = lobby.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found in lobby");

            player.IsReady = request.IsReady;

            return Ok(lobby);
        }
    }

    [HttpPost("{lobbyId}/start")]
    public ActionResult StartGame(string lobbyId, [FromBody] StartGameRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            if (lobby.CreatorId != request.CreatorId)
                return Forbid("Only lobby creator can start the game");

            if (lobby.Status != LobbyStatus.Waiting)
                return BadRequest("Lobby is not ready to start");

            if (lobby.Players.Count < 2)
                return BadRequest("Need at least 2 players to start");

            // Проверяем, что все игроки готовы (опционально)
            if (request.RequireAllReady && lobby.Players.Any(p => !p.IsReady))
                return BadRequest("Not all players are ready");

            lobby.Status = LobbyStatus.Starting;

            // Здесь можно создать матч
            // Например, вызвать MatchController для создания игры

            return Ok(lobby);
        }
    }

    [HttpPost("{lobbyId}/settings")]
    public ActionResult UpdateLobbySettings(string lobbyId, [FromBody] UpdateLobbySettingsRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            if (lobby.CreatorId != request.CreatorId)
                return Forbid("Only lobby creator can update settings");

            if (lobby.Status != LobbyStatus.Waiting)
                return BadRequest("Cannot update settings while game is starting or in progress");

            if (!string.IsNullOrEmpty(request.Name))
                lobby.Name = request.Name;

            if (request.MaxPlayers.HasValue)
            {
                if (request.MaxPlayers.Value < lobby.Players.Count)
                    return BadRequest("Cannot reduce max players below current player count");
                
                lobby.MaxPlayers = request.MaxPlayers.Value;
            }

            if (request.IsPublic.HasValue)
                lobby.IsPublic = request.IsPublic.Value;

            if (request.Password != null)
                lobby.Password = request.Password;

            return Ok(lobby);
        }
    }

    [HttpPost("{lobbyId}/kick")]
    public ActionResult KickPlayer(string lobbyId, [FromBody] KickPlayerRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            if (lobby.CreatorId != request.CreatorId)
                return Forbid("Only lobby creator can kick players");

            if (request.PlayerId == request.CreatorId)
                return BadRequest("Cannot kick yourself");

            var player = lobby.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found in lobby");

            lobby.Players.Remove(player);
            return Ok(lobby);
        }
    }

    [HttpGet("{lobbyId}/status")]
    public ActionResult<LobbyStatusResponse> GetLobbyStatus(string lobbyId)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            var status = new LobbyStatusResponse
            {
                LobbyId = lobby.Id,
                Name = lobby.Name,
                Status = lobby.Status,
                PlayerCount = lobby.Players.Count,
                MaxPlayers = lobby.MaxPlayers,
                IsPublic = lobby.IsPublic,
                CreatedAt = lobby.CreatedAt,
                Players = lobby.Players.Select(p => new PlayerStatusDto
                {
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    IsReady = p.IsReady,
                    Score = p.Score
                }).ToList()
            };

            return Ok(status);
        }
    }

    [HttpDelete("{lobbyId}")]
    public ActionResult DeleteLobby(string lobbyId, [FromBody] DeleteLobbyRequest request)
    {
        lock (LobbyLock)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
                return NotFound();

            if (lobby.CreatorId != request.CreatorId)
                return Forbid("Only lobby creator can delete lobby");

            Lobbies.Remove(lobbyId);
            return NoContent();
        }
    }
}

public class CreateLobbyRequest
{
    public string Name { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

public class JoinLobbyRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class LeaveLobbyRequest
{
    public string PlayerId { get; set; } = string.Empty;
}



public class StartGameRequest
{
    public string CreatorId { get; set; } = string.Empty;
    public bool RequireAllReady { get; set; } = false;
}

public class UpdateLobbySettingsRequest
{
    public string CreatorId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? MaxPlayers { get; set; }
    public bool? IsPublic { get; set; }
    public string? Password { get; set; }
}

public class KickPlayerRequest
{
    public string CreatorId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
}

public class DeleteLobbyRequest
{
    public string CreatorId { get; set; } = string.Empty;
}

public class LobbyStatusResponse
{
    public string LobbyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LobbyStatus Status { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PlayerStatusDto> Players { get; set; } = new();
} 