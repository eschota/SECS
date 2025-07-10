namespace Server.Models;

public class Game
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameStatus Status { get; set; }
    public List<GamePlayer> Players { get; set; } = new();
    public string? WinnerId { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class GamePlayer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsReady { get; set; }
    public int Score { get; set; }
}

public class QueueEntry
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public QueueStatus Status { get; set; }
    public string? GameId { get; set; }
}

public class Lobby
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public LobbyStatus Status { get; set; }
    public List<GamePlayer> Players { get; set; } = new();
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

public class Match
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GamePlayer> Players { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public MatchStatus Status { get; set; }
    public string? WinnerId { get; set; }
    public int Duration { get; set; }
    public Dictionary<string, object> GameData { get; set; } = new();
}

public enum GameStatus
{
    Waiting,
    InProgress,
    Finished,
    Cancelled
}

public enum QueueStatus
{
    Waiting,
    GameFound,
    InGame,
    Cancelled
}

public enum LobbyStatus
{
    Waiting,
    Starting,
    InGame,
    Finished
}

public enum MatchStatus
{
    Starting,
    InProgress,
    Finished,
    Cancelled
} 