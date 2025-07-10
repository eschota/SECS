using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Server.Models;

public enum GameMatchType
{
    OneVsOne = 1,       // 1x1
    TwoVsTwo = 2,       // 2x2
    FourPlayerFFA = 4   // 1x1x1x1 (Free For All)
}

public enum GameMatchStatus
{
    InProgress,
    Completed,
    Cancelled
}

public class GameMatch
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MatchId { get; set; }

    [Required]
    public GameMatchType MatchType { get; set; }

    [Required]
    public string PlayersId { get; set; } = string.Empty; // JSON массив int[]

    [Required]
    public string TeamId { get; set; } = string.Empty; // JSON массив int[]

    [Required]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public string? MatchWin { get; set; } // JSON массив int[]

    public string? MatchLose { get; set; } // JSON массив int[]

    public string? MatchDraw { get; set; } // JSON массив int[]

    [Required]
    public float MatchMaxTimeLimit { get; set; } = 60.0f; // 1 минута в секундах

    [Required]
    public GameMatchStatus Status { get; set; } = GameMatchStatus.InProgress;

    // Вспомогательные методы для работы с JSON массивами
    [NotMapped]
    public List<int> PlayersList
    {
        get => string.IsNullOrEmpty(PlayersId) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(PlayersId) ?? new List<int>();
        set => PlayersId = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<int> TeamsList
    {
        get => string.IsNullOrEmpty(TeamId) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(TeamId) ?? new List<int>();
        set => TeamId = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<int> WinnersList
    {
        get => string.IsNullOrEmpty(MatchWin) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(MatchWin) ?? new List<int>();
        set => MatchWin = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<int> LosersList
    {
        get => string.IsNullOrEmpty(MatchLose) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(MatchLose) ?? new List<int>();
        set => MatchLose = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<int> DrawList
    {
        get => string.IsNullOrEmpty(MatchDraw) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(MatchDraw) ?? new List<int>();
        set => MatchDraw = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > StartTime.AddSeconds(MatchMaxTimeLimit);
} 