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

        // Настройка модели GameMatch
        modelBuilder.Entity<GameMatch>()
            .Property(m => m.MatchId)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.StartTime)
            .HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.MatchMaxTimeLimit)
            .HasDefaultValue(600.0f);

        modelBuilder.Entity<GameMatch>()
            .Property(m => m.Status)
            .HasDefaultValue(GameMatchStatus.InProgress);

        // Индексы для производительности
        modelBuilder.Entity<GameMatch>()
            .HasIndex(m => m.Status);
    }
} 