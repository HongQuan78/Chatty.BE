using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password.Trim(), workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
    }
}
