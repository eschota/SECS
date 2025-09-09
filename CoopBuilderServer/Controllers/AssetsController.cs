using Microsoft.AspNetCore.Mvc;
using CoopBuilderServer.Services;

namespace CoopBuilderServer.Controllers;

[ApiController]
[Route("api-game-assets")]
public class AssetsController : ControllerBase
{
    private readonly AssetIndexService _index;

    public AssetsController(AssetIndexService index)
    {
        _index = index;
    }

    [HttpGet("search")]
    public ActionResult<object> Search([FromQuery] string? q, [FromQuery] int? category_id, [FromQuery] int? sub_category_id, [FromQuery] bool? is_character, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var (items, total) = _index.Search(q, category_id, sub_category_id, is_character, limit, offset);
        return Ok(new { items, total });
    }

    [HttpGet("{guid}/meta")]
    public ActionResult<object> Meta(string guid)
    {
        var item = _index.Entries.FirstOrDefault(e => string.Equals(e.asset_guid, guid, StringComparison.OrdinalIgnoreCase));
        if (item is null) return NotFound();
        return Ok(item);
    }

    // GET /api-game-assets/count?q=...&category_id=...&sub_category_id=...&is_character=...
    [HttpGet("count")]
    public ActionResult<object> Count([FromQuery] string? q, [FromQuery] int? category_id, [FromQuery] int? sub_category_id, [FromQuery] bool? is_character)
    {
        var total = _index.Count(q, category_id, sub_category_id, is_character);
        return Ok(new { total });
    }

    // GET /api-game-assets/count-by-category?q=... (вернет по категориям и подкатегориям)
    [HttpGet("count-by-category")]
    public ActionResult<object> CountByCategory([FromQuery] string? q, [FromQuery] int? category_id, [FromQuery] int? sub_category_id, [FromQuery] bool? is_character)
    {
        var groups = _index.CategoryCounts(q, category_id, sub_category_id, is_character);
        return Ok(new { items = groups, total = groups.Sum(g => g.total) });
    }
}


