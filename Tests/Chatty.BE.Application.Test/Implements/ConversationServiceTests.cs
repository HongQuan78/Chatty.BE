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

    public ConversationServiceTests()
    {
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
            _objectMapper.Object
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
        var conversation = await service.CreatePrivateConversationAsync(userA, userB);

        // Assert
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
        _notificationService.Verify(
            n =>
                n.NotifyUserJoinedConversationAsync(
                    conversation.Id,
                    userA,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _notificationService.Verify(
            n =>
                n.NotifyUserJoinedConversationAsync(
                    conversation.Id,
                    userB,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        var conversation = await service.CreatePrivateConversationAsync(userA, userB);

        // Assert
        Assert.Equal(existing.Id, conversation.Id);
        _conversationRepository.Verify(
            r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _notificationService.Verify(
            n =>
                n.NotifyUserJoinedConversationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task CreatePrivateConversationAsync_ShouldThrow_WhenUserIdsSame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreatePrivateConversationAsync(userId, userId)
        );

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

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });
        _userRepository
            .Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _participantRepository
            .Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        await service.AddParticipantAsync(conversationId, userId);

        // Assert
        _participantRepository.Verify(
            r => r.AddParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            n =>
                n.NotifyUserJoinedConversationAsync(
                    conversationId,
                    userId,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldSkip_WhenAlreadyParticipant()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });
        _userRepository
            .Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _participantRepository
            .Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        await service.AddParticipantAsync(conversationId, userId);

        // Assert
        _participantRepository.Verify(
            r =>
                r.AddParticipantAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _notificationService.Verify(
            n =>
                n.NotifyUserJoinedConversationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldThrow_WhenConversationMissing()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddParticipantAsync(conversationId, userId)
        );

        _userRepository.Verify(
            r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveParticipantAsync_ShouldRemove_WhenUserBelongsToConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });
        _participantRepository
            .Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        await service.RemoveParticipantAsync(conversationId, userId);

        // Assert
        _participantRepository.Verify(
            r => r.RemoveParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            n =>
                n.NotifyUserLeftConversationAsync(
                    conversationId,
                    userId,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
