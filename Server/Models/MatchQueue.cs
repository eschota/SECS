using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class MatchQueue
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int QueueId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public GameMatchType MatchType { get; set; }

    [Required]
    public int MmrRating { get; set; }

    [Required]
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;

    [Required]
    public int SearchThreshold { get; set; } = 20; // Начальный порог поиска

    // Навигационное свойство
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // Вычисляемые свойства
    [NotMapped]
    public TimeSpan TimeInQueue => DateTime.UtcNow - JoinTime;

    public int CalculateCurrentMmrThreshold()
    {
        var secondsInQueue = (int)TimeInQueue.TotalSeconds;
        var expansions = secondsInQueue / 10; // Каждые 10 секунд
        var expandedThreshold = SearchThreshold + (int)(MmrRating * 0.1f * expansions);
        return Math.Max(expandedThreshold, 100); // Минимум 100 единиц
    }
} 