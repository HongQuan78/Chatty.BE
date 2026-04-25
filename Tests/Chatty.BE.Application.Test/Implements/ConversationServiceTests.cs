using Chatty.BE.Application.Common;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class ConversationServiceTests
{
    private readonly Mock<IConversationRepository> _conversationRepository = new();
    private readonly Mock<IConversationParticipantRepository> _participantRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IObjectMapper> _objectMapper = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public ConversationServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        _objectMapper
            .Setup(m => m.Map<ConversationDto>(It.IsAny<Conversation>()))
            .Returns<Conversation>(c => new ConversationDto
            {
                Id = c.Id,
                Name = c.Name,
                OwnerId = c.OwnerId,
                IsGroup = c.IsGroup,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            });

        _objectMapper
            .Setup(m => m.Map<List<ConversationDto>>(It.IsAny<IReadOnlyList<Conversation>>()))
            .Returns<IReadOnlyList<Conversation>>(list =>
                list.Select(c => new ConversationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    OwnerId = c.OwnerId,
                    IsGroup = c.IsGroup,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                }).ToList()
            );
    }

    private ConversationService CreateService() =>
        new(
            _conversationRepository.Object,
            _participantRepository.Object,
            _userRepository.Object,
            _notificationService.Object,
            _unitOfWork.Object,
            _objectMapper.Object,
            _dateTimeProvider.Object
        );

    [Fact]
    public async Task CreatePrivateConversationAsync_ShouldCreateConversation_WhenUsersValid()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _userRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conversationRepository
            .Setup(r => r.GetPrivateConversationAsync(userA, userB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var service = CreateService();

        // Act
        var result = await service.CreatePrivateConversationAsync(userA, userB);

        // Assert
        Assert.True(result.IsSuccess);
        var conversation = result.Value!;
        Assert.False(conversation.IsGroup);
        Assert.NotEqual(Guid.Empty, conversation.Id);

        _conversationRepository.Verify(
            r =>
                r.AddAsync(
                    It.Is<Conversation>(c => c.Id == conversation.Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _participantRepository.Verify(
            p => p.AddParticipantAsync(conversation.Id, userA, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _participantRepository.Verify(
            p => p.AddParticipantAsync(conversation.Id, userB, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreatePrivateConversationAsync_ShouldReturnExisting_WhenConversationAlreadyExists()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var existing = new Conversation { Id = Guid.NewGuid() };

        _userRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conversationRepository
            .Setup(r => r.GetPrivateConversationAsync(userA, userB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var service = CreateService();

        // Act
        var result = await service.CreatePrivateConversationAsync(userA, userB);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value!.Id);
        _conversationRepository.Verify(
            r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreatePrivateConversationAsync_ShouldReturnFailure_WhenUserIdsSame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.CreatePrivateConversationAsync(userId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("BAD_REQUEST", result.ErrorCode);

        _userRepository.Verify(
            r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldAddAndNotify_WhenUserNotYetParticipant()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId, IsGroup = true, OwnerId = actorId });
        _userRepository
            .Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _participantRepository
            .Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.AddParticipantAsync(conversationId, userId, actorId);

        // Assert
        Assert.True(result.IsSuccess);
        _participantRepository.Verify(
            r => r.AddParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldReturnForbidden_WhenActorNotOwnerOfGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId, IsGroup = true, OwnerId = ownerId });

        var service = CreateService();

        // Act
        var result = await service.AddParticipantAsync(conversationId, userId, actorId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldReturnNotFound_WhenConversationMissing()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var service = CreateService();

        // Act
        var result = await service.AddParticipantAsync(conversationId, userId, actorId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task RemoveParticipantAsync_ShouldRemove_WhenUserBelongsToConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = userId; // User removing themselves

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId, IsGroup = true, OwnerId = Guid.NewGuid() });
        _participantRepository
            .Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.RemoveParticipantAsync(conversationId, userId, actorId);

        // Assert
        Assert.True(result.IsSuccess);
        _participantRepository.Verify(
            r => r.RemoveParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
