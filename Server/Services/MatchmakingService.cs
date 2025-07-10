using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public class MatchmakingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchmakingService> _logger;
    private readonly TimeSpan _matchmakingInterval = TimeSpan.FromSeconds(10);

    public MatchmakingService(IServiceProvider serviceProvider, ILogger<MatchmakingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

                await ProcessMatchmaking(context);
                await ProcessTimeouts(context);

                await Task.Delay(_matchmakingInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in matchmaking service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessMatchmaking(GameDbContext context)
    {
        // Обрабатываем 1v1 матчи
        await ProcessOneVsOneMatches(context);
        
        // Обрабатываем 2v2 матчи
        await ProcessTwoVsTwoMatches(context);
        
        // Обрабатываем 4-player FFA матчи
        await ProcessFourPlayerFFAMatches(context);
    }

    private async Task ProcessOneVsOneMatches(GameDbContext context)
    {
        var players = await context.MatchQueues
            .Where(q => q.MatchType == GameMatchType.OneVsOne)
            .Include(q => q.User)
            .OrderBy(q => q.JoinTime)
            .ToListAsync();

        for (int i = 0; i < players.Count - 1; i++)
        {
            var player1 = players[i];
            
            for (int j = i + 1; j < players.Count; j++)
            {
                var player2 = players[j];
                
                // Проверяем MMR совместимость
                var mmrDiff = Math.Abs(player1.MmrRating - player2.MmrRating);
                var threshold1 = player1.CalculateCurrentMmrThreshold();
                var threshold2 = player2.CalculateCurrentMmrThreshold();
                
                if (mmrDiff <= Math.Min(threshold1, threshold2))
                {
                    // Создаем матч
                    var match = new GameMatch
                    {
                        MatchType = GameMatchType.OneVsOne,
                        PlayersList = new List<int> { player1.UserId, player2.UserId },
                        TeamsList = new List<int> { 1, 2 }, // Разные команды
                        StartTime = DateTime.UtcNow,
                        Status = GameMatchStatus.InProgress
                    };

                    context.GameMatches.Add(match);
                    
                    // Обновляем игроков
                    player1.User.CurrentMatchId = match.MatchId;
                    player1.User.IsInQueue = false;
                    player2.User.CurrentMatchId = match.MatchId;
                    player2.User.IsInQueue = false;
                    
                    // Удаляем из очереди
                    context.MatchQueues.Remove(player1);
                    context.MatchQueues.Remove(player2);
                    
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Created 1v1 match between {player1.UserId} and {player2.UserId}");
                    
                    // Удаляем из локального списка
                    players.Remove(player1);
                    players.Remove(player2);
                    i--; // Компенсируем изменение индекса
                    break;
                }
            }
        }
    }

    private async Task ProcessTwoVsTwoMatches(GameDbContext context)
    {
        var players = await context.MatchQueues
            .Where(q => q.MatchType == GameMatchType.TwoVsTwo)
            .Include(q => q.User)
            .OrderBy(q => q.JoinTime)
            .ToListAsync();

        if (players.Count < 4) return;

        for (int i = 0; i < players.Count - 3; i++)
        {
            var team1Players = new List<MatchQueue> { players[i] };
            var team2Players = new List<MatchQueue>();

            // Ищем подходящих игроков для первой команды
            for (int j = i + 1; j < players.Count && team1Players.Count < 2; j++)
            {
                var candidate = players[j];
                var avgMmr1 = team1Players.Average(p => p.MmrRating);
                var threshold = team1Players.Min(p => p.CalculateCurrentMmrThreshold());

                if (Math.Abs(candidate.MmrRating - avgMmr1) <= threshold)
                {
                    team1Players.Add(candidate);
                }
            }

            if (team1Players.Count < 2) continue;

            // Ищем игроков для второй команды
            var remainingPlayers = players.Except(team1Players).ToList();
            
            for (int j = 0; j < remainingPlayers.Count - 1 && team2Players.Count < 2; j++)
            {
                var player1 = remainingPlayers[j];
                var player2 = remainingPlayers[j + 1];
                
                var avgMmr1 = team1Players.Average(p => p.MmrRating);
                var avgMmr2 = new[] { player1.MmrRating, player2.MmrRating }.Average();
                var threshold = Math.Min(team1Players.Min(p => p.CalculateCurrentMmrThreshold()),
                                       Math.Min(player1.CalculateCurrentMmrThreshold(), player2.CalculateCurrentMmrThreshold()));

                if (Math.Abs(avgMmr1 - avgMmr2) <= threshold)
                {
                    team2Players.Add(player1);
                    team2Players.Add(player2);
                }
            }

            if (team2Players.Count == 2)
            {
                // Создаем матч
                var allPlayers = team1Players.Concat(team2Players).ToList();
                var match = new GameMatch
                {
                    MatchType = GameMatchType.TwoVsTwo,
                    PlayersList = allPlayers.Select(p => p.UserId).ToList(),
                    TeamsList = new List<int> { 1, 1, 2, 2 },
                    StartTime = DateTime.UtcNow,
                    Status = GameMatchStatus.InProgress
                };

                context.GameMatches.Add(match);
                
                // Обновляем игроков
                foreach (var player in allPlayers)
                {
                    player.User.CurrentMatchId = match.MatchId;
                    player.User.IsInQueue = false;
                    context.MatchQueues.Remove(player);
                }
                
                await context.SaveChangesAsync();
                
                _logger.LogInformation($"Created 2v2 match with players: {string.Join(", ", allPlayers.Select(p => p.UserId))}");
                
                // Удаляем из локального списка
                foreach (var player in allPlayers)
                {
                    players.Remove(player);
                }
                i--; // Компенсируем изменение индекса
            }
        }
    }

    private async Task ProcessFourPlayerFFAMatches(GameDbContext context)
    {
        var players = await context.MatchQueues
            .Where(q => q.MatchType == GameMatchType.FourPlayerFFA)
            .Include(q => q.User)
            .OrderBy(q => q.JoinTime)
            .ToListAsync();

        if (players.Count < 4) return;

        for (int i = 0; i < players.Count - 3; i++)
        {
            var matchPlayers = new List<MatchQueue> { players[i] };
            var baseMmr = players[i].MmrRating;
            var threshold = players[i].CalculateCurrentMmrThreshold();

            // Ищем 3 других игроков с подходящим MMR
            for (int j = i + 1; j < players.Count && matchPlayers.Count < 4; j++)
            {
                var candidate = players[j];
                
                if (Math.Abs(candidate.MmrRating - baseMmr) <= threshold)
                {
                    matchPlayers.Add(candidate);
                }
            }

            if (matchPlayers.Count == 4)
            {
                // Создаем матч
                var match = new GameMatch
                {
                    MatchType = GameMatchType.FourPlayerFFA,
                    PlayersList = matchPlayers.Select(p => p.UserId).ToList(),
                    TeamsList = new List<int> { 1, 2, 3, 4 }, // Каждый сам за себя
                    StartTime = DateTime.UtcNow,
                    Status = GameMatchStatus.InProgress
                };

                context.GameMatches.Add(match);
                
                // Обновляем игроков
                foreach (var player in matchPlayers)
                {
                    player.User.CurrentMatchId = match.MatchId;
                    player.User.IsInQueue = false;
                    context.MatchQueues.Remove(player);
                }
                
                await context.SaveChangesAsync();
                
                _logger.LogInformation($"Created 4-player FFA match with players: {string.Join(", ", matchPlayers.Select(p => p.UserId))}");
                
                // Удаляем из локального списка
                foreach (var player in matchPlayers)
                {
                    players.Remove(player);
                }
                i--; // Компенсируем изменение индекса
            }
        }
    }

    private async Task ProcessTimeouts(GameDbContext context)
    {
        // 1. Обрабатываем истекшие матчи
        var expiredMatches = await context.GameMatches
            .Where(m => m.Status == GameMatchStatus.InProgress)
            .ToListAsync();
        
        // Фильтруем истекшие матчи в памяти
        expiredMatches = expiredMatches.Where(m => m.IsExpired).ToList();

        foreach (var match in expiredMatches)
        {
            match.Status = GameMatchStatus.Completed;
            match.EndTime = DateTime.UtcNow;

            // Выбираем случайного победителя
            var random = new Random();
            var winnerIndex = random.Next(match.PlayersList.Count);
            var winnerId = match.PlayersList[winnerIndex];

            match.WinnersList = new List<int> { winnerId };
            match.LosersList = match.PlayersList.Where(id => id != winnerId).ToList();

            // Обновляем статистики игроков (MMR обновляется здесь же)
            await UpdatePlayerStatsOnTimeout(context, match);

            _logger.LogInformation($"Match {match.MatchId} timed out, random winner: {winnerId}");
        }

        // 2. КРИТИЧЕСКИ ВАЖНО: Очищаем очередь от неактивных игроков
        await ProcessInactivePlayersInQueue(context);

        if (expiredMatches.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessInactivePlayersInQueue(GameDbContext context)
    {
        // Определяем время неактивности (3 минуты без heartbeat)
        var inactiveThreshold = DateTime.UtcNow.AddMinutes(-3);
        
        // Находим игроков в очереди, которые неактивны
        var inactiveQueueEntries = await context.MatchQueues
            .Include(q => q.User)
            .Where(q => q.User.LastHeartbeat == null || q.User.LastHeartbeat < inactiveThreshold)
            .ToListAsync();

        if (inactiveQueueEntries.Any())
        {
            foreach (var entry in inactiveQueueEntries)
            {
                // Удаляем из очереди
                context.MatchQueues.Remove(entry);
                
                // Обновляем статус игрока
                entry.User.IsInQueue = false;
                entry.User.IsActive = false;
                
                _logger.LogInformation($"Removed inactive player {entry.User.Username} (ID: {entry.UserId}) from queue. Last heartbeat: {entry.User.LastHeartbeat}");
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {inactiveQueueEntries.Count} inactive players from queue");
        }
    }

    private async Task UpdatePlayerStatsOnTimeout(GameDbContext context, GameMatch match)
    {
        var players = await context.Users
            .Where(u => match.PlayersList.Contains(u.Id))
            .ToListAsync();

        foreach (var player in players)
        {
            // Обновляем MMR при timeout матча
            int mmrChange = 0;

            if (match.WinnersList.Contains(player.Id))
            {
                mmrChange = +20; // Победа
                player.GamesWon++;
            }
            else if (match.LosersList.Contains(player.Id))
            {
                mmrChange = -20; // Поражение
            }
            else
            {
                mmrChange = +5; // Ничья
            }

            // Обновляем MMR для соответствующего типа матча
            switch (match.MatchType)
            {
                case GameMatchType.OneVsOne:
                    player.MmrOneVsOne = Math.Max(500, player.MmrOneVsOne + mmrChange);
                    break;
                case GameMatchType.TwoVsTwo:
                    player.MmrTwoVsTwo = Math.Max(500, player.MmrTwoVsTwo + mmrChange);
                    break;
                case GameMatchType.FourPlayerFFA:
                    player.MmrFourPlayerFFA = Math.Max(500, player.MmrFourPlayerFFA + mmrChange);
                    break;
            }

            // Обновляем общую статистику
            player.GamesPlayed++;
            player.CurrentMatchId = null;
        }
    }
} 