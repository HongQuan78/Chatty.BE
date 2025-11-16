using System.Security.Cryptography;
using System.Text;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Implements;

public class AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<User> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedUserName = userName.Trim();

        if (await _userRepository.IsEmailTakenAsync(normalizedEmail, ct))
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        if (await _userRepository.IsUserNameTakenAsync(normalizedUserName, ct))
        {
            throw new InvalidOperationException("Username is already in use.");
        }

        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return user;
    }

    public async Task<(User user, string accessToken)> LoginAsync(
        string userNameOrEmail,
        string password,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userNameOrEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        User? user = null;
        if (userNameOrEmail.Contains('@', StringComparison.Ordinal))
        {
            user = await _userRepository.GetByEmailAsync(userNameOrEmail.Trim(), ct);
        }

        user ??= await _userRepository.GetByUserNameAsync(userNameOrEmail.Trim(), ct);

        if (user is null || !VerifyPassword(password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var accessToken = GenerateAccessToken(user);
        return (user, accessToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user =
            await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!VerifyPassword(currentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32
        );

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string hashString)
    {
        var parts = hashString.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            storedHash.Length
        );

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    private static string GenerateAccessToken(User user)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes);
    }
}
