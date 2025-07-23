namespace Server.Models;

/// <summary>
/// In-memory DTO для очереди матчмейкинга. Не является Entity Framework моделью.
/// </summary>
public class MatchQueue
{
    public int QueueId { get; set; }
    public int UserId { get; set; }
    public GameMatchType MatchType { get; set; }
    public int MmrRating { get; set; }
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;
    public int SearchThreshold { get; set; } = 20; // Начальный порог поиска

    // Вычисляемые свойства
    public TimeSpan TimeInQueue => DateTime.UtcNow - JoinTime;

    public int CalculateCurrentMmrThreshold()
    {
        var secondsInQueue = (int)TimeInQueue.TotalSeconds;
        var expansions = secondsInQueue / 10; // Каждые 10 секунд
        var expandedThreshold = SearchThreshold + (int)(MmrRating * 0.1f * expansions);
        return Math.Max(expandedThreshold, 100); // Минимум 100 единиц
    }
} 