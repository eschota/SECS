using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api-game-online")]
public class OnlineGameController : ControllerBase
{
    [HttpGet]
    public IActionResult GetGamePage()
    {
        var html = ReadFileContent("wwwroot/online-game/game.html");
        
        // Модифицируем HTML для динамической загрузки стилей и скриптов
        html = html.Replace("href=\"game.css\"", "href=\"/api-game-online/styles\"")
                   .Replace("src=\"game.js\"", "src=\"/api-game-online/scripts\"");
        
        return Content(html, "text/html");
    }

    [HttpGet("styles")]
    public IActionResult GetStyles()
    {
        var css = ReadFileContent("wwwroot/online-game/game.css");
        return Content(css, "text/css");
    }

    [HttpGet("scripts")]
    public IActionResult GetScripts()
    {
        var js = ReadFileContent("wwwroot/online-game/game.js");
        return Content(js, "application/javascript");
    }

    private string ReadFileContent(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            if (System.IO.File.Exists(fullPath))
            {
                return System.IO.File.ReadAllText(fullPath);
            }
            return "/* Файл не найден */";
        }
        catch (Exception ex)
        {
            return $"/* Ошибка загрузки файла: {ex.Message} */";
        }
    }
} 