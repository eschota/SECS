using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Setup NLog logging
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Add Entity Framework with SQLite
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite("Data Source=gameserver.db"));

// Add custom services
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<InMemoryMatchmakingService>();
builder.Services.AddHostedService<MatchmakingService>();

// Add CORS policy for game clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("GamePolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("GamePolicy");

// Serve static files from wwwroot
app.UseStaticFiles();

// Serve static files from wwwroot/online-game for /online-game path
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "online-game")),
    RequestPath = "/online-game"
});

app.UseRouting();
app.UseAuthorization();

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<GameHub>("/gameHub");

// Очистка очереди от неактивных игроков при запуске
await CleanupInactivePlayersOnStartup(app);

app.Run();

// Метод для очистки очереди от неактивных игроков при запуске
async Task CleanupInactivePlayersOnStartup(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // 1. ОЧИЩАЕМ ВСЕ АКТИВНЫЕ МАТЧИ ПРИ ЗАПУСКЕ
        var activeMatches = await context.GameMatches
            .Where(m => m.Status == GameMatchStatus.InProgress)
            .ToListAsync();
        
        if (activeMatches.Any())
        {
            foreach (var match in activeMatches)
            {
                match.Status = GameMatchStatus.Cancelled;
                match.EndTime = DateTime.UtcNow;
                logger.LogInformation($"🗑️ Cancelled active match {match.MatchId} (type: {match.MatchType}) at startup");
            }
            
            logger.LogInformation($"🧹 Startup cleanup: Cancelled {activeMatches.Count} active matches");
        }
        
        // 2. ОЧИЩАЕМ ВСЕХ ИГРОКОВ ОТ МАТЧЕЙ ПРИ ЗАПУСКЕ (больше не нужно, так как CurrentMatchId убрали)
        logger.LogInformation("🧹 Startup cleanup: Skipping player match cleanup (CurrentMatchId removed from model)");

        // 3. ОЧЕРЕДЬ ТЕПЕРЬ ТОЛЬКО В ПАМЯТИ - НЕ НУЖНО ОЧИЩАТЬ БАЗУ
        logger.LogInformation("🧹 Startup cleanup: Skipping queue cleanup (queues are now in-memory only)");
        
        // Сохраняем все изменения
        await context.SaveChangesAsync();
        
        logger.LogInformation("✅ Startup cleanup completed successfully");
    }
    catch (Exception ex)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error during startup cleanup");
    }
}

// SignalR Hub для реалтайм взаимодействия
public class GameHub : Hub
{
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("PlayerJoined", Context.ConnectionId);
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId);
    }

    public async Task SendGameAction(string gameId, string action, object data)
    {
        await Clients.Group(gameId).SendAsync("GameAction", Context.ConnectionId, action, data);
    }

    public async Task JoinLobby(string lobbyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby_{lobbyId}");
        await Clients.Group($"lobby_{lobbyId}").SendAsync("PlayerJoinedLobby", Context.ConnectionId);
    }

    public async Task LeaveLobby(string lobbyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby_{lobbyId}");
        await Clients.Group($"lobby_{lobbyId}").SendAsync("PlayerLeftLobby", Context.ConnectionId);
    }

    public async Task SendLobbyMessage(string lobbyId, string message)
    {
        await Clients.Group($"lobby_{lobbyId}").SendAsync("LobbyMessage", Context.ConnectionId, message);
    }

    public async Task JoinMatch(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match_{matchId}");
        await Clients.Group($"match_{matchId}").SendAsync("PlayerJoinedMatch", Context.ConnectionId);
    }

    public async Task LeaveMatch(string matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match_{matchId}");
        await Clients.Group($"match_{matchId}").SendAsync("PlayerLeftMatch", Context.ConnectionId);
    }

    public async Task SendMatchAction(string matchId, string action, object data)
    {
        await Clients.Group($"match_{matchId}").SendAsync("MatchAction", Context.ConnectionId, action, data);
    }
}
