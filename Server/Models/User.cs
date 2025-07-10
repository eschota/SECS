using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Avatar { get; set; }

    [StringLength(255)]
    public string? OAuthToken { get; set; }

    [StringLength(50)]
    public string? OAuthProvider { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastHeartbeat { get; set; }

    public bool IsActive { get; set; } = true;

    // Игровые статистики
    public int GamesPlayed { get; set; } = 0;
    public int GamesWon { get; set; } = 0;
    public int Score { get; set; } = 0;
    public int Level { get; set; } = 1;

    // MMR рейтинги для разных типов матчей
    public int MmrOneVsOne { get; set; } = 500;      // 1x1
    public int MmrTwoVsTwo { get; set; } = 500;      // 2x2  
    public int MmrFourPlayerFFA { get; set; } = 500; // 1x1x1x1

    // Текущий статус игрока
    public int? CurrentMatchId { get; set; }       // ID текущего матча (null если не в матче)
    public bool IsInQueue { get; set; } = false;   // В очереди поиска игры
} 