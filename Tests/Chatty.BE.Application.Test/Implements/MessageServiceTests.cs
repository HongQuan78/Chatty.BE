using Chatty.BE.Application.Implements;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.DTOs.MessageAttachments;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
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
    private readonly Mock<IObjectMapper> _objectMapper = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IValidator<SendMessageRequest>> _validator = new();

    public MessageServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        _objectMapper
            .Setup(m => m.Map<Message>(It.IsAny<Message>()))
            .Returns<Message>(m => m);
        _objectMapper
            .Setup(m => m.Map<MessageDto>(It.IsAny<Message>()))
            .Returns<Message>(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                Content = m.Content,
                Type = m.Type,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
            });

        // Default to valid
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private MessageService CreateService() =>
        new(
            _messageRepository.Object,
            _attachmentRepository.Object,
            _receiptRepository.Object,
            _conversationRepository.Object,
            _participantRepository.Object,
            _notificationService.Object,
            _unitOfWork.Object,
            _objectMapper.Object,
            _dateTimeProvider.Object,
            _validator.Object
        );

    [Fact]
    public async Task SendMessageAsync_ShouldPersistMessageAndNotify_WhenConversationValid()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var attachments = new List<CreateMessageAttachmentRequest>
        {
            new()
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
        var request = new SendMessageRequest
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = "Hello",
            Type = MessageType.Text,
            Attachments = attachments
        };

        // Act
        var message = await service.SendMessageAsync(request);

        // Assert
        Assert.True(message.IsSuccess);
        Assert.Equal(conversationId, message.Value!.ConversationId);
        Assert.Equal(senderId, message.Value!.SenderId);

        _messageRepository.Verify(
            r => r.AddAsync(It.Is<Message>(m => m.Id == message.Value!.Id), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _attachmentRepository.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<MessageAttachment>>(list =>
                        list.Count() == attachments.Count
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _receiptRepository.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<MessageReceipt>>(list =>
                        list.Count() == 2
                        && list.Any(receipt => receipt.UserId == recipientId)
                        && list.Any(receipt => receipt.UserId == senderId)
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
                    It.Is<Message>(m => m.Id == message.Value!.Id),
                    It.Is<IEnumerable<Guid>>(ids => ids.OrderBy(x => x).SequenceEqual(new[] { senderId, recipientId }.OrderBy(x => x))),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFailure_WhenSenderNotInConversation()
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
        var request = new SendMessageRequest
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = "Hello",
            Type = MessageType.Text
        };

        // Act
        var result = await service.SendMessageAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);

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
        var result = await service.MarkConversationAsReadAsync(conversationId, userId);

        // Assert
        Assert.True(result.IsSuccess);

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
    public async Task MarkConversationAsReadAsync_ShouldReturnFailure_WhenUserNotParticipant()
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

        // Act
        var result = await service.MarkConversationAsReadAsync(conversationId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);

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
