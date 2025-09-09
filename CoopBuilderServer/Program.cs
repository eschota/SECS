using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using CoopBuilderServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind settings
var serverSettings = builder.Configuration.GetSection("ServerSettings");
var apiBasePath = serverSettings.GetValue<string>("ApiBasePath") ?? "/api-game";
var allowedOrigins = serverSettings.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Services
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Db
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

// Asset index
builder.Services.AddSingleton<AssetIndexService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }
        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

// App
var app = builder.Build();

app.UsePathBase(apiBasePath);
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");
app.MapControllers();

// Ensure database exists and init asset index on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    var index = scope.ServiceProvider.GetRequiredService<AssetIndexService>();
}

app.Run();

// EF Core DbContext and models (MVP)
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<SceneChange> SceneChanges => Set<SceneChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().HasKey(p => p.PlayerId);
        modelBuilder.Entity<Player>().Property(p => p.PlayerId).ValueGeneratedOnAdd();
        modelBuilder.Entity<Player>().Property(p => p.PlayerName).HasDefaultValue("");
        modelBuilder.Entity<Player>().Property(p => p.PlayerAvatarUrl).HasDefaultValue("");

        modelBuilder.Entity<PlayerStats>().HasKey(s => s.PlayerId);
        modelBuilder.Entity<PlayerStats>().Property(s => s.ValuesJson).HasDefaultValue("{}");

        modelBuilder.Entity<Scene>().HasKey(s => s.SceneId);
        modelBuilder.Entity<Scene>().Property(s => s.SceneId).ValueGeneratedOnAdd();
        modelBuilder.Entity<Scene>().Property(s => s.CreationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Scene>().Property(s => s.LastUpdateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<SceneChange>().HasKey(c => new { c.SceneId, c.ChangeId });
        modelBuilder.Entity<SceneChange>().Property(c => c.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

public class Player
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerAvatarUrl { get; set; } = string.Empty;
}

public class PlayerStats
{
    public int PlayerId { get; set; }
    public string ValuesJson { get; set; } = "{}";
}

public class Scene
{
    public int SceneId { get; set; }
    public int OwnerPlayerId { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string ScenePreviewUrl { get; set; } = string.Empty;
    public string WhoLikedSceneJson { get; set; } = "[]";
    public string WhoDislikedSceneJson { get; set; } = "[]";
    public string AssetsJson { get; set; } = "[]";
}

public class SceneChange
{
    public int SceneId { get; set; }
    public long ChangeId { get; set; }
    public DateTime Timestamp { get; set; }
    public int ActorPlayerId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}
