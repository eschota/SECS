using System.Collections.Concurrent;
using Server.Models;

namespace Server.Services;

/// <summary>
/// In-memory сервис для хранения очередей и активных матчей. Не связан с базой данных.
/// После рестарта сервера все очереди и активные матчи сбрасываются.
/// </summary>
public class InMemoryMatchmakingService
{
    // Очереди поиска по типу матча (in-memory)
    private readonly ConcurrentDictionary<GameMatchType, List<MatchQueue>> _queues = new();
    // Активные матчи (in-memory)
    private readonly ConcurrentDictionary<int, GameMatch> _activeMatches = new();
    private int _nextMatchId = 1;
    private readonly object _idLock = new();

    public InMemoryMatchmakingService()
    {
        // Инициализация очередей по типам
        foreach (GameMatchType type in Enum.GetValues(typeof(GameMatchType)))
        {
            _queues[type] = new List<MatchQueue>();
        }
    }

    /// <summary>
    /// Добавить игрока в очередь поиска (in-memory).
    /// </summary>
    public void AddToQueue(MatchQueue queue)
    {
        lock (_queues[queue.MatchType])
        {
            _queues[queue.MatchType].Add(queue);
        }
    }

    /// <summary>
    /// Удалить игрока из очереди поиска (in-memory).
    /// </summary>
    public void RemoveFromQueue(int userId, GameMatchType type)
    {
        lock (_queues[type])
        {
            _queues[type].RemoveAll(q => q.UserId == userId);
        }
    }

    /// <summary>
    /// Получить список игроков в очереди по типу матча (in-memory).
    /// </summary>
    public List<MatchQueue> GetQueue(GameMatchType type)
    {
        lock (_queues[type])
        {
            return _queues[type].ToList();
        }
    }

    /// <summary>
    /// Создать новый активный матч (in-memory).
    /// </summary>
    public int CreateMatch(GameMatch match)
    {
        lock (_idLock)
        {
            match.MatchId = _nextMatchId++;
        }
        match.Status = GameMatchStatus.InProgress;
        _activeMatches[match.MatchId] = match;
        return match.MatchId;
    }

    /// <summary>
    /// Получить активный матч по ID (in-memory).
    /// </summary>
    public bool TryGetActiveMatch(int matchId, out GameMatch match) => _activeMatches.TryGetValue(matchId, out match);

    /// <summary>
    /// Получить все активные матчи пользователя (in-memory).
    /// </summary>
    public List<GameMatch> GetUserActiveMatches(int userId)
    {
        return _activeMatches.Values.Where(m => m.PlayersList.Contains(userId) && m.Status == GameMatchStatus.InProgress).ToList();
    }

    /// <summary>
    /// Завершить матч и удалить его из активных (in-memory).
    /// </summary>
    public void FinishMatch(int matchId, GameMatchStatus status)
    {
        if (_activeMatches.TryGetValue(matchId, out var match))
        {
            match.Status = status;
            match.EndTime = DateTime.UtcNow;
            _activeMatches.TryRemove(matchId, out var _);
        }
    }

    /// <summary>
    /// Получить все активные матчи (in-memory).
    /// </summary>
    public IEnumerable<GameMatch> GetAllActiveMatches() => _activeMatches.Values.ToList();
    public List<GameMatch> GetActiveMatches() => _activeMatches.Values.ToList();

    /// <summary>
    /// Очистить все очереди и активные матчи (in-memory).
    /// </summary>
    public void ClearAll()
    {
        foreach (var type in _queues.Keys)
        {
            lock (_queues[type])
            {
                _queues[type].Clear();
            }
        }
        _activeMatches.Clear();
    }
} 