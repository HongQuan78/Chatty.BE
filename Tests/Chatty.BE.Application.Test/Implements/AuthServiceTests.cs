using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<IObjectMapper> _objectMapper = new();
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator = new();
    private readonly Mock<IValidator<LoginRequestDto>> _loginValidator = new();
    private readonly Mock<IValidator<ChangePasswordRequest>> _changePasswordValidator = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
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

        _objectMapper
            .Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns<User>(u => new UserDto { Id = u.Id, UserName = u.UserName, Email = u.Email });

        // Default to valid
        _registerValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _loginValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _changePasswordValidator.Setup(v => v.ValidateAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private AuthService CreateService() =>
        new(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _tokenProvider.Object,
            _dateTimeProvider.Object,
            _unitOfWork.Object,
            _objectMapper.Object,
            _registerValidator.Object,
            _loginValidator.Object,
            _changePasswordValidator.Object,
            _logger.Object
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
        var request = new RegisterRequest { UserName = "Alice", Email = "user@example.com", Password = "P@ssw0rd!" };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("alice", result.Value!.UserName.ToLowerInvariant());
        Assert.Equal("user@example.com", result.Value.Email);
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
            .Setup(r =>
                r.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(storedUser);
        _passwordHasher.Setup(p => p.VerifyPassword("correct", "secure")).Returns(true);

        var service = CreateService();

        // Act
        var result = await service.LoginAsync(
            new LoginRequestDto { Email = "user@example.com", Password = "correct" },
            "127.0.0.1"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(storedUser.Id, result.Value!.UserId);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUnauthorized_WhenPasswordInvalid()
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
            .Setup(r =>
                r.GetByEmailForUpdateAsync("user@example.com", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(storedUser);
        _passwordHasher.Setup(p => p.VerifyPassword("wrong", "secure")).Returns(false);
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "wrong" }, "127.0.0.1");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
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
        var request = new ChangePasswordRequest { UserId = user.Id, CurrentPassword = "old", NewPassword = "new" };

        // Act
        var result = await service.ChangePasswordAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("hashed::new", user.PasswordHash);
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
