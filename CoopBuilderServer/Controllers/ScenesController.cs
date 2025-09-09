using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoopBuilderServer.Controllers;

[ApiController]
[Route("api-game-scenes")]
public class ScenesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ScenesController(AppDbContext db)
    {
        _db = db;
    }

    public class CreateSceneRequest
    {
        public int owner_player_id { get; set; }
        public string? scene_name { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Scene>> Create([FromBody] CreateSceneRequest request)
    {
        var owner = await _db.Players.FindAsync(request.owner_player_id);
        if (owner == null) return NotFound("owner_player_id not found");

        var scene = new Scene
        {
            OwnerPlayerId = request.owner_player_id,
            SceneName = request.scene_name ?? string.Empty,
            ScenePreviewUrl = string.Empty,
            WhoLikedSceneJson = "[]",
            WhoDislikedSceneJson = "[]",
            AssetsJson = "[]",
            CreationDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow
        };
        _db.Scenes.Add(scene);
        await _db.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(scene.SceneName))
        {
            scene.SceneName = scene.SceneId.ToString();
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = scene.SceneId }, scene);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Scene>> GetById(int id)
    {
        var scene = await _db.Scenes.FindAsync(id);
        if (scene == null) return NotFound();
        return Ok(scene);
    }

    public class SceneChangeRequest
    {
        public int actor_player_id { get; set; }
        public string operation { get; set; } = string.Empty;
        public object? payload { get; set; }
    }

    [HttpGet("{id}/changes")]
    public async Task<ActionResult<object>> GetChanges(int id, [FromQuery] long? since_change_id, [FromQuery] int limit = 200)
    {
        var sceneExists = await _db.Scenes.AsNoTracking().AnyAsync(s => s.SceneId == id);
        if (!sceneExists) return NotFound();

        var query = _db.SceneChanges.AsNoTracking().Where(c => c.SceneId == id);
        if (since_change_id.HasValue)
        {
            query = query.Where(c => c.ChangeId > since_change_id.Value);
        }
        var items = await query.OrderBy(c => c.ChangeId).Take(Math.Clamp(limit, 1, 1000)).ToListAsync();
        var last = items.LastOrDefault()?.ChangeId ?? since_change_id ?? 0;
        return Ok(new { items, last_change_id = last });
    }

    [HttpPost("{id}/changes")]
    public async Task<ActionResult<object>> AddChange(int id, [FromBody] SceneChangeRequest request)
    {
        var scene = await _db.Scenes.FindAsync(id);
        if (scene == null) return NotFound();

        var lastId = await _db.SceneChanges.Where(c => c.SceneId == id).MaxAsync(c => (long?)c.ChangeId) ?? 0;
        var change = new SceneChange
        {
            SceneId = id,
            ChangeId = lastId + 1,
            Timestamp = DateTime.UtcNow,
            ActorPlayerId = request.actor_player_id,
            Operation = request.operation,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(request.payload ?? new { })
        };
        _db.SceneChanges.Add(change);
        scene.LastUpdateDate = change.Timestamp;
        await _db.SaveChangesAsync();
        return Created($"/api-game-scenes/{id}/changes?since_change_id={change.ChangeId}", new { change_id = change.ChangeId, timestamp = change.Timestamp });
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<object>> JoinScene(int id, [FromBody] dynamic body)
    {
        var exists = await _db.Scenes.AnyAsync(s => s.SceneId == id);
        if (!exists) return NotFound();
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/leave")]
    public async Task<ActionResult<object>> LeaveScene(int id, [FromBody] dynamic body)
    {
        var exists = await _db.Scenes.AnyAsync(s => s.SceneId == id);
        if (!exists) return NotFound();
        return Ok(new { ok = true });
    }
}


