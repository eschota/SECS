namespace Server.Models;

// PlayerController DTOs
public class CreatePlayerRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdatePlayerRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class UpdateStatsRequest
{
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int Score { get; set; }
    public TimeSpan PlayTime { get; set; }
}

// Common DTOs
public class PlayerStatusDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsReady { get; set; }
}

public class PlayerRequestDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

// Common request classes
public class SetPlayerReadyRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public bool IsReady { get; set; }
} 