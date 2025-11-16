using System.Security.Cryptography;
using System.Text;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AuthService CreateService() => new(_userRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task RegisterAsync_ShouldPersistUser_WhenDataValid()
    {
        // Arrange
        _userRepository
            .Setup(r => r.IsEmailTakenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository
            .Setup(r => r.IsUserNameTakenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var user = await service.RegisterAsync(" Alice ", "USER@Example.Com ", "Sup3rSecret!");

        // Assert
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Alice", user.UserName);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));

        _userRepository.Verify(
            r =>
                r.AddAsync(
                    It.Is<User>(u =>
                        u.Email == "user@example.com" && u.UserName == "Alice" && u.Id != Guid.Empty
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyTaken()
    {
        // Arrange
        _userRepository
            .Setup(r => r.IsEmailTakenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync("Alice", "alice@example.com", "pass123")
        );

        _userRepository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUserAndToken_WhenCredentialsMatch()
    {
        // Arrange
        var hashedPassword = CreatePasswordHash("Correct horse");
        var storedUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "rider",
            Email = "r@e.com",
            PasswordHash = hashedPassword,
        };

        _userRepository
            .Setup(r => r.GetByEmailAsync("r@e.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedUser);

        var service = CreateService();

        // Act
        var (user, token) = await service.LoginAsync("r@e.com", "Correct horse");

        // Assert
        Assert.Same(storedUser, user);
        Assert.False(string.IsNullOrWhiteSpace(token));
        _userRepository.Verify(
            r => r.GetByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var storedUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "rider",
            Email = "r@e.com",
            PasswordHash = CreatePasswordHash("Correct horse"),
        };

        _userRepository
            .Setup(r => r.GetByUserNameAsync("rider", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedUser);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("rider", "wrong")
        );
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdateHash_WhenCurrentPasswordMatches()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), PasswordHash = CreatePasswordHash("current") };
        var originalHash = user.PasswordHash;

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        // Act
        await service.ChangePasswordAsync(user.Id, "current", "new pass");

        // Assert
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotEqual(originalHash, user.PasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordInvalid()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), PasswordHash = CreatePasswordHash("current") };

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(user.Id, "bad", "new")
        );

        _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string CreatePasswordHash(string password)
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
}
