using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public class MatchmakingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchmakingService> _logger;
    private readonly InMemoryMatchmakingService _memory;
    private readonly TimeSpan _matchmakingInterval = TimeSpan.FromSeconds(10);

    public MatchmakingService(IServiceProvider serviceProvider, ILogger<MatchmakingService> logger, InMemoryMatchmakingService memory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _memory = memory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ProcessMatchmaking();
                await Task.Delay(_matchmakingInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in matchmaking service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private void ProcessMatchmaking()
    {
        // 1v1
        ProcessOneVsOneMatches();
        // 2v2
        ProcessTwoVsTwoMatches();
        // FFA
        ProcessFourPlayerFFAMatches();
    }

    private void ProcessOneVsOneMatches()
    {
        var players = _memory.GetQueue(GameMatchType.OneVsOne);
        for (int i = 0; i < players.Count - 1; i++)
        {
            var player1 = players[i];
            for (int j = i + 1; j < players.Count; j++)
            {
                var player2 = players[j];
                var mmrDiff = Math.Abs(player1.MmrRating - player2.MmrRating);
                var threshold1 = player1.CalculateCurrentMmrThreshold();
                var threshold2 = player2.CalculateCurrentMmrThreshold();
                if (mmrDiff <= Math.Min(threshold1, threshold2))
                {
                    var match = new GameMatch
                    {
                        MatchType = GameMatchType.OneVsOne,
                        PlayersList = new List<int> { player1.UserId, player2.UserId },
                        TeamsList = new List<int> { 1, 2 },
                        StartTime = DateTime.UtcNow,
                        Status = GameMatchStatus.InProgress
                    };
                    _memory.CreateMatch(match);
                    _memory.RemoveFromQueue(player1.UserId, GameMatchType.OneVsOne);
                    _memory.RemoveFromQueue(player2.UserId, GameMatchType.OneVsOne);
                    _logger.LogInformation($"[InMemory] Created 1v1 match between {player1.UserId} and {player2.UserId}");
                    break;
                }
            }
        }
    }

    private void ProcessTwoVsTwoMatches()
    {
        var players = _memory.GetQueue(GameMatchType.TwoVsTwo);
        if (players.Count < 4) return;
        for (int i = 0; i < players.Count - 3; i++)
        {
            var team1Players = new List<MatchQueue> { players[i] };
            var team2Players = new List<MatchQueue>();
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
                var allPlayers = team1Players.Concat(team2Players).ToList();
                var match = new GameMatch
                {
                    MatchType = GameMatchType.TwoVsTwo,
                    PlayersList = allPlayers.Select(p => p.UserId).ToList(),
                    TeamsList = new List<int> { 1, 1, 2, 2 },
                    StartTime = DateTime.UtcNow,
                    Status = GameMatchStatus.InProgress
                };
                _memory.CreateMatch(match);
                foreach (var player in allPlayers)
                {
                    _memory.RemoveFromQueue(player.UserId, GameMatchType.TwoVsTwo);
                }
                _logger.LogInformation($"[InMemory] Created 2v2 match with players: {string.Join(", ", allPlayers.Select(p => p.UserId))}");
            }
        }
    }

    private void ProcessFourPlayerFFAMatches()
    {
        var players = _memory.GetQueue(GameMatchType.FourPlayerFFA);
        if (players.Count < 4) return;
        for (int i = 0; i < players.Count - 3; i++)
        {
            var matchPlayers = new List<MatchQueue> { players[i] };
            var baseMmr = players[i].MmrRating;
            var threshold = players[i].CalculateCurrentMmrThreshold();
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
                var match = new GameMatch
                {
                    MatchType = GameMatchType.FourPlayerFFA,
                    PlayersList = matchPlayers.Select(p => p.UserId).ToList(),
                    TeamsList = new List<int> { 1, 2, 3, 4 },
                    StartTime = DateTime.UtcNow,
                    Status = GameMatchStatus.InProgress
                };
                _memory.CreateMatch(match);
                foreach (var player in matchPlayers)
                {
                    _memory.RemoveFromQueue(player.UserId, GameMatchType.FourPlayerFFA);
                }
                _logger.LogInformation($"[InMemory] Created 4-player FFA match with players: {string.Join(", ", matchPlayers.Select(p => p.UserId))}");
            }
        }
    }

    // --- Завершение матчей ---
    public async Task FinishMatch(int matchId, GameMatchStatus status)
    {
        if (!_memory.TryGetActiveMatch(matchId, out var match)) return;
        _memory.FinishMatch(matchId, status);
        // Сохраняем завершённый матч в базу
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        match.Status = status;
        match.EndTime = DateTime.UtcNow;
        context.GameMatches.Add(match);
        await context.SaveChangesAsync();
        _logger.LogInformation($"[InMemory] Finished match {matchId} with status {status} and saved to DB");
    }
} 