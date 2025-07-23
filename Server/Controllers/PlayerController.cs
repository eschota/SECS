using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using System.ComponentModel.DataAnnotations;

namespace Server.Controllers;

[ApiController]
[Route("api-game-player")]
public class PlayerController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly AuthService _authService;

    public PlayerController(GameDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // GET: api-game-player
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetPlayers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Avatar = u.Avatar,
                CreatedAt = u.CreatedAt,
                GamesPlayed = u.GamesPlayed,
                GamesWon = u.GamesWon,
                Score = u.Score,
                Level = u.Level,
                MmrOneVsOne = u.MmrOneVsOne,
                MmrTwoVsTwo = u.MmrTwoVsTwo,
                MmrFourPlayerFFA = u.MmrFourPlayerFFA
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api-game-player/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetPlayer(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
        {
            return NotFound($"Пользователь с ID {id} не найден");
        }

        // 💓 Автоматическое обновление heartbeat при запросе статуса игрока
        user.LastHeartbeat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            GamesPlayed = user.GamesPlayed,
            GamesWon = user.GamesWon,
            Score = user.Score,
            Level = user.Level,
            MmrOneVsOne = user.MmrOneVsOne,
            MmrTwoVsTwo = user.MmrTwoVsTwo,
            MmrFourPlayerFFA = user.MmrFourPlayerFFA
        };

        return Ok(userDto);
    }

    // POST: api-game-player
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreatePlayer(CreateUserDto createUserDto)
    {
        // Валидация данных
        if (!_authService.IsValidEmail(createUserDto.Email))
        {
            return BadRequest("Некорректный формат email");
        }

        if (!_authService.IsValidPassword(createUserDto.Password))
        {
            return BadRequest("Пароль должен содержать минимум 6 символов");
        }

        // Проверка уникальности email
        var existingUserByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest("Пользователь с таким email уже существует");
        }

        // Проверка уникальности username
        var existingUserByUsername = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == createUserDto.Username);
        if (existingUserByUsername != null)
        {
            return BadRequest("Пользователь с таким именем уже существует");
        }

        // Создание нового пользователя
        var user = new User
        {
            Email = createUserDto.Email,
            Username = createUserDto.Username,
            PasswordHash = _authService.HashPassword(createUserDto.Password),
            Avatar = createUserDto.Avatar ?? "https://www.gravatar.com/avatar/?d=mp",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow, // Устанавливаем начальный heartbeat
            
            // Явно устанавливаем MMR 500 для всех типов матчей
            MmrOneVsOne = 500,
            MmrTwoVsTwo = 500,
            MmrFourPlayerFFA = 500
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            GamesPlayed = user.GamesPlayed,
            GamesWon = user.GamesWon,
            Score = user.Score,
            Level = user.Level,
            MmrOneVsOne = user.MmrOneVsOne,
            MmrTwoVsTwo = user.MmrTwoVsTwo,
            MmrFourPlayerFFA = user.MmrFourPlayerFFA
        };

        return CreatedAtAction(nameof(GetPlayer), new { id = user.Id }, userDto);
    }

    // POST: api-game-player/login
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        // Валидация данных
        if (!_authService.IsValidEmail(loginDto.Email))
        {
            return BadRequest("Некорректный формат email");
        }

        // Поиск пользователя по email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

        if (user == null)
        {
            return BadRequest("Неверный email или пароль");
        }

        // Проверка пароля
        if (!_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return BadRequest("Неверный email или пароль");
        }

        // Обновление времени последнего входа
        user.LastLoginAt = DateTime.UtcNow;
        user.LastHeartbeat = DateTime.UtcNow; // Устанавливаем heartbeat при логине
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            GamesPlayed = user.GamesPlayed,
            GamesWon = user.GamesWon,
            Score = user.Score,
            Level = user.Level,
            MmrOneVsOne = user.MmrOneVsOne,
            MmrTwoVsTwo = user.MmrTwoVsTwo,
            MmrFourPlayerFFA = user.MmrFourPlayerFFA
        };

        return Ok(userDto);
    }

    // PUT: api-game-player/5
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdatePlayer(int id, UpdateUserDto updateUserDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
        {
            return NotFound($"Пользователь с ID {id} не найден");
        }

        // Обновление данных
        if (!string.IsNullOrEmpty(updateUserDto.Username))
        {
            // Проверка уникальности нового username
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == updateUserDto.Username && u.Id != id);
            if (existingUser != null)
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }
            user.Username = updateUserDto.Username;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Avatar))
        {
            user.Avatar = updateUserDto.Avatar;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Password))
        {
            if (!_authService.IsValidPassword(updateUserDto.Password))
            {
                return BadRequest("Пароль должен содержать минимум 6 символов");
            }
            user.PasswordHash = _authService.HashPassword(updateUserDto.Password);
        }

        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            GamesPlayed = user.GamesPlayed,
            GamesWon = user.GamesWon,
            Score = user.Score,
            Level = user.Level,
            MmrOneVsOne = user.MmrOneVsOne,
            MmrTwoVsTwo = user.MmrTwoVsTwo,
            MmrFourPlayerFFA = user.MmrFourPlayerFFA
        };

        return Ok(userDto);
    }

    // DELETE: api-game-player/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Пользователь с ID {id} не найден");
        }

        // Мягкое удаление
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs для передачи данных
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int Score { get; set; }
    public int Level { get; set; }
    
    // MMR рейтинги
    public int MmrOneVsOne { get; set; }
    public int MmrTwoVsTwo { get; set; }
    public int MmrFourPlayerFFA { get; set; }
}

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Url]
    public string? Avatar { get; set; }
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    [MaxLength(100)]
    public string? Username { get; set; }
    
    [Url]
    public string? Avatar { get; set; }
    
    [MinLength(6)]
    public string? Password { get; set; }
} 