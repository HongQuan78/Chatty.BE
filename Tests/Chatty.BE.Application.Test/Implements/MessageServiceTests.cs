using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using Moq;

namespace Chatty.BE.Application.Test.Implements;

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _messageRepository = new();
    private readonly Mock<IMessageAttachmentRepository> _attachmentRepository = new();
    private readonly Mock<IMessageReceiptRepository> _receiptRepository = new();
    private readonly Mock<IConversationRepository> _conversationRepository = new();
    private readonly Mock<IConversationParticipantRepository> _participantRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private MessageService CreateService() =>
        new(
            _messageRepository.Object,
            _attachmentRepository.Object,
            _receiptRepository.Object,
            _conversationRepository.Object,
            _participantRepository.Object,
            _notificationService.Object,
            _unitOfWork.Object
        );

    [Fact]
    public async Task SendMessageAsync_ShouldPersistMessageAndNotify_WhenConversationValid()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var attachments = new[]
        {
            new MessageAttachment
            {
                FileName = "file.txt",
                FileUrl = "http://x",
                ContentType = "text/plain",
                FileSizeBytes = 10,
            },
        };

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });
        _conversationRepository
            .Setup(r =>
                r.UserIsInConversationAsync(conversationId, senderId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _participantRepository
            .Setup(r => r.GetParticipantsAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<User>
                {
                    new() { Id = senderId },
                    new() { Id = recipientId },
                }
            );

        var service = CreateService();

        // Act
        var message = await service.SendMessageAsync(
            conversationId,
            senderId,
            "Hello",
            MessageType.Text,
            attachments
        );

        // Assert
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId, message.SenderId);

        _messageRepository.Verify(
            r => r.AddAsync(It.Is<Message>(m => m.Id == message.Id), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _attachmentRepository.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<MessageAttachment>>(list =>
                        list.Count() == attachments.Length
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _receiptRepository.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<MessageReceipt>>(list =>
                        list.Count() == 1 && list.All(receipt => receipt.UserId == recipientId)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _conversationRepository.Verify(r => r.Update(It.IsAny<Conversation>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            n =>
                n.NotifyMessageSentAsync(
                    It.Is<Message>(m => m.Id == message.Id),
                    It.Is<IEnumerable<Guid>>(ids => ids.Single() == recipientId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrow_WhenSenderNotInConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        _conversationRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });
        _conversationRepository
            .Setup(r =>
                r.UserIsInConversationAsync(conversationId, senderId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(false);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendMessageAsync(
                conversationId,
                senderId,
                "Hello",
                MessageType.Text,
                Enumerable.Empty<MessageAttachment>()
            )
        );

        _messageRepository.Verify(
            r => r.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MarkConversationAsReadAsync_ShouldMarkReceiptsAndNotify_WhenUnreadExists()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var unreadMessages = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _conversationRepository
            .Setup(r =>
                r.UserIsInConversationAsync(conversationId, userId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _receiptRepository
            .Setup(r =>
                r.GetUnreadMessageIdsForUserAsync(
                    conversationId,
                    userId,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(unreadMessages);

        var service = CreateService();

        // Act
        await service.MarkConversationAsReadAsync(conversationId, userId);

        // Assert
        foreach (var messageId in unreadMessages)
        {
            _receiptRepository.Verify(
                r => r.MarkAsReadAsync(messageId, userId, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(
            n =>
                n.NotifyMessagesReadAsync(
                    conversationId,
                    userId,
                    It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(unreadMessages)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task MarkConversationAsReadAsync_ShouldThrow_WhenUserNotParticipant()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _conversationRepository
            .Setup(r =>
                r.UserIsInConversationAsync(conversationId, userId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(false);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MarkConversationAsReadAsync(conversationId, userId)
        );

        _receiptRepository.Verify(
            r =>
                r.GetUnreadMessageIdsForUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}
