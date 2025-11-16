using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DateTime _now = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public AuthServiceTests()
    {
        _dateTimeProvider.SetupGet(p => p.UtcNow).Returns(() => _now);

        _passwordHasher
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns<string>(password => $"hashed::{password}");

        _passwordHasher
            .Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _tokenProvider
            .Setup(p => p.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AccessTokenResult("access-token", _now.AddMinutes(5)));

        _tokenProvider
            .Setup(p => p.GenerateRefreshToken(It.IsAny<Guid>()))
            .Returns(new RefreshTokenResult("refresh-token", _now.AddDays(7)));

        _tokenProvider
            .Setup(p => p.ComputeHash(It.IsAny<string>()))
            .Returns<string>(token => $"hash::{token}");
    }

    private AuthService CreateService() =>
        new(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _tokenProvider.Object,
            _dateTimeProvider.Object,
            _unitOfWork.Object
        );

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
        var user = await service.RegisterAsync("Alice", "USER@example.com", "P@ssw0rd!");

        // Assert
        Assert.Equal("alice", user.UserName.ToLowerInvariant());
        Assert.Equal("user@example.com", user.Email);
        _passwordHasher.Verify(p => p.HashPassword("P@ssw0rd!"), Times.Once);
        _userRepository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsMatch()
    {
        // Arrange
        var storedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "alice",
            PasswordHash = "secure",
        };

        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedUser);
        _passwordHasher.Setup(p => p.VerifyPassword("correct", "secure")).Returns(true);

        var service = CreateService();

        // Act
        var response = await service.LoginAsync(
            new LoginRequestDto("user@example.com", "correct"),
            "127.0.0.1"
        );

        // Assert
        Assert.Equal(storedUser.Id, response.UserId);
        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordInvalid()
    {
        // Arrange
        var storedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "alice",
            PasswordHash = "secure",
        };
        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedUser);
        _passwordHasher.Setup(p => p.VerifyPassword("wrong", "secure")).Returns(false);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginRequestDto("user@example.com", "wrong"), "127.0.0.1")
        );
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdateHash_WhenCurrentPasswordMatches()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), PasswordHash = "hashed::old" };
        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.VerifyPassword("old", "hashed::old")).Returns(true);
        _passwordHasher.Setup(p => p.HashPassword("new")).Returns("hashed::new");
        var service = CreateService();

        // Act
        await service.ChangePasswordAsync(user.Id, "old", "new");

        // Assert
        Assert.Equal("hashed::new", user.PasswordHash);
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
