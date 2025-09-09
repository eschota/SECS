using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoopBuilderServer.Controllers;

[ApiController]
[Route("api-game-player")]
public class PlayerController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlayerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Player>>> GetAll()
    {
        var players = await _db.Players.AsNoTracking().ToListAsync();
        return Ok(players);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> GetById(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player == null) return NotFound();
        return Ok(player);
    }

    public class CreatePlayerRequest
    {
        public string? player_name { get; set; }
        public string? player_avatar_url { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Player>> Create([FromBody] CreatePlayerRequest request)
    {
        var entity = new Player
        {
            PlayerName = request.player_name ?? string.Empty,
            PlayerAvatarUrl = request.player_avatar_url ?? string.Empty
        };
        _db.Players.Add(entity);
        await _db.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(entity.PlayerName))
        {
            entity.PlayerName = entity.PlayerId.ToString();
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.PlayerId }, entity);
    }

    public class UpdatePlayerRequest
    {
        public string? player_name { get; set; }
        public string? player_avatar_url { get; set; }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Player>> Update(int id, [FromBody] UpdatePlayerRequest request)
    {
        var entity = await _db.Players.FindAsync(id);
        if (entity == null) return NotFound();

        if (request.player_name != null) entity.PlayerName = request.player_name;
        if (request.player_avatar_url != null) entity.PlayerAvatarUrl = request.player_avatar_url;
        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Players.FindAsync(id);
        if (entity == null) return NotFound();
        _db.Players.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Stats
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<object>> GetStats(int id)
    {
        var stats = await _db.PlayerStats.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == id);
        if (stats == null) return NotFound();
        var json = System.Text.Json.JsonDocument.Parse(stats.ValuesJson).RootElement.Clone();
        return Ok(json);
    }

    [HttpPost("{id}/stats")]
    public async Task<ActionResult<object>> SetStats(int id, [FromBody] object payload)
    {
        var player = await _db.Players.FindAsync(id);
        if (player == null) return NotFound();

        var valuesJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var stats = await _db.PlayerStats.FirstOrDefaultAsync(s => s.PlayerId == id);
        if (stats == null)
        {
            stats = new PlayerStats { PlayerId = id, ValuesJson = valuesJson };
            _db.PlayerStats.Add(stats);
        }
        else
        {
            stats.ValuesJson = valuesJson;
        }
        await _db.SaveChangesAsync();

        var json = System.Text.Json.JsonDocument.Parse(valuesJson).RootElement.Clone();
        return Ok(json);
    }
}


