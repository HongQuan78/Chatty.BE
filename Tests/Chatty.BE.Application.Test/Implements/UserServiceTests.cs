using Chatty.BE.Application.Implements;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IObjectMapper> _objectMapper = new();

    public UserServiceTests()
    {
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
    }

    private UserService CreateService() =>
        new(_userRepository.Object, _unitOfWork.Object, _objectMapper.Object);

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

        // Act
        var result = await service.UpdateProfileAsync(
            userId,
            "  New Name ",
            " https://cdn/avatar.png ",
            "  Hello world  "
        );

        // Assert
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("https://cdn/avatar.png", result.AvatarUrl);
        Assert.Equal("Hello world", result.Bio);

        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenUserCannotBeFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateProfileAsync(userId, "name", "avatar", "bio")
        );

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

        // Act
        var result = await service.UpdateProfileAsync(userId, null, null, null);

        // Assert
        Assert.Equal("Current", result.DisplayName);
        Assert.Equal("current", result.AvatarUrl);
        Assert.Equal("current", result.Bio);
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }
}
