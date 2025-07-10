using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<GameMatch> GameMatches { get; set; }
    public DbSet<MatchQueue> MatchQueues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка уникальных индексов
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Настройка автоинкремента для Id
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedOnAdd();

        // Настройка значений по умолчанию
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<User>()
            .Property(u => u.LastLoginAt)
            .HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.GamesPlayed)
            .HasDefaultValue(0);

        modelBuilder.Entity<User>()
            .Property(u => u.GamesWon)
            .HasDefaultValue(0);

        modelBuilder.Entity<User>()
            .Property(u => u.Score)
            .HasDefaultValue(0);

        modelBuilder.Entity<User>()
            .Property(u => u.Level)
            .HasDefaultValue(1);

        // Настройка MMR рейтингов
        modelBuilder.Entity<User>()
            .Property(u => u.MmrOneVsOne)
            .HasDefaultValue(500);

        modelBuilder.Entity<User>()
            .Property(u => u.MmrTwoVsTwo)
            .HasDefaultValue(500);

        modelBuilder.Entity<User>()
            .Property(u => u.MmrFourPlayerFFA)
            .HasDefaultValue(500);

        modelBuilder.Entity<User>()
            .Property(u => u.IsInQueue)
            .HasDefaultValue(false);

        // Настройка модели GameMatch
        modelBuilder.Entity<GameMatch>()
            .Property(m => m.MatchId)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.StartTime)
            .HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.MatchMaxTimeLimit)
            .HasDefaultValue(60.0f);

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.Status)
            .HasDefaultValue(GameMatchStatus.InProgress);

        // Настройка модели MatchQueue
        modelBuilder.Entity<MatchQueue>()
            .Property(mq => mq.QueueId)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<MatchQueue>()
            .Property(mq => mq.JoinTime)
            .HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<MatchQueue>()
            .Property(mq => mq.SearchThreshold)
            .HasDefaultValue(20);

        // Индексы для производительности
        modelBuilder.Entity<MatchQueue>()
            .HasIndex(mq => mq.UserId)
            .IsUnique();

        modelBuilder.Entity<MatchQueue>()
            .HasIndex(mq => new { mq.MatchType, mq.MmrRating });

        modelBuilder.Entity<GameMatch>()
            .HasIndex(m => m.Status);

        // Внешние ключи
        modelBuilder.Entity<MatchQueue>()
            .HasOne(mq => mq.User)
            .WithMany()
            .HasForeignKey(mq => mq.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 