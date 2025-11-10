using System.Security.Cryptography;
using System.Text;

namespace MinimalApiDemo.Services;

public class PasswordHasher
{
    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hash = HashPassword(password);
        return hash == hashedPassword;
    }
}
