using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IObjectMapper> _objectMapper = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IValidator<UpdateUserProfileRequest>> _validator = new();

    public UserServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        _objectMapper
            .Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns<User>(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                CreatedAt = u.CreatedAt,
            });

        // Default to valid
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private UserService CreateService() =>
        new(
            _userRepository.Object,
            _unitOfWork.Object,
            _objectMapper.Object,
            _dateTimeProvider.Object,
            _validator.Object
        );

    [Fact]
    public async Task UpdateProfileAsync_ShouldTrimValuesAndPersist_WhenInputsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            DisplayName = "Old",
            AvatarUrl = "old",
            Bio = "old",
        };

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();
        var request = new UpdateUserProfileRequest
        {
            UserId = userId,
            DisplayName = "  New Name ",
            AvatarUrl = " https://cdn/avatar.png ",
            Bio = "  Hello world  "
        };

        // Act
        var result = await service.UpdateProfileAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.DisplayName);
        Assert.Equal("https://cdn/avatar.png", result.Value.AvatarUrl);
        Assert.Equal("Hello world", result.Value.Bio);

        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnNotFound_WhenUserMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();
        var request = new UpdateUserProfileRequest { UserId = userId, DisplayName = "name" };

        // Act
        var result = await service.UpdateProfileAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldKeepExistingValues_WhenOptionalInputsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            DisplayName = "Current",
            AvatarUrl = "current",
            Bio = "current",
        };

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();
        var request = new UpdateUserProfileRequest { UserId = userId };

        // Act
        var result = await service.UpdateProfileAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Current", result.Value!.DisplayName);
        Assert.Equal("current", result.Value.AvatarUrl);
        Assert.Equal("current", result.Value.Bio);
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@test.com" };

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }
}
