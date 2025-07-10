using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

public class AuthService
{
    /// <summary>
    /// Хеширует пароль с использованием SHA-256 и соли
    /// </summary>
    public string HashPassword(string password, string salt = "")
    {
        if (string.IsNullOrEmpty(salt))
        {
            salt = GenerateSalt();
        }

        using var sha256 = SHA256.Create();
        var saltedPassword = password + salt;
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        var hashedPassword = Convert.ToBase64String(hashedBytes);
        
        // Возвращаем хеш с солью для последующей проверки
        return $"{salt}:{hashedPassword}";
    }

    /// <summary>
    /// Проверяет соответствие пароля хешу
    /// </summary>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash) || !storedHash.Contains(':'))
        {
            return false;
        }

        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = parts[0];
        var expectedHash = parts[1];
        
        var actualHash = HashPassword(password, salt);
        var actualHashPart = actualHash.Split(':')[1];
        
        return actualHashPart == expectedHash;
    }

    /// <summary>
    /// Генерирует случайную соль
    /// </summary>
    private string GenerateSalt()
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Генерирует случайный токен для OAuth
    /// </summary>
    public string GenerateOAuthToken()
    {
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Проверяет валидность email
    /// </summary>
    public bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Проверяет валидность пароля
    /// </summary>
    public bool IsValidPassword(string password)
    {
        return !string.IsNullOrEmpty(password) && password.Length >= 6;
    }
} 