using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.Implements;

public class MessageService(
    IMessageRepository messageRepository,
    IMessageAttachmentRepository attachmentRepository,
    IMessageReceiptRepository receiptRepository,
    IConversationRepository conversationRepository,
    IConversationParticipantRepository participantRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork
) : IMessageService
{
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly IMessageAttachmentRepository _attachmentRepository = attachmentRepository;
    private readonly IMessageReceiptRepository _receiptRepository = receiptRepository;
    private readonly IConversationRepository _conversationRepository = conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository =
        participantRepository;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<IReadOnlyList<Message>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    ) => _messageRepository.GetMessagesAsync(conversationId, page, pageSize, ct);

    public Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    ) => _messageRepository.CountUnreadMessagesAsync(conversationId, userId, ct);

    public async Task<Message> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        string content,
        MessageType type,
        IEnumerable<MessageAttachment>? attachments,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        var conversation =
            await _conversationRepository.GetByIdAsync(conversationId, ct)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} was not found.");

        var isParticipant = await _conversationRepository.UserIsInConversationAsync(
            conversationId,
            senderId,
            ct
        );
        if (!isParticipant)
        {
            throw new InvalidOperationException("Sender is not a member of the conversation.");
        }

        var utcNow = DateTime.UtcNow;
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            Type = type,
            Status = MessageStatus.Sent,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await _messageRepository.AddAsync(message, ct);

        if (attachments is not null)
        {
            var preparedAttachments = attachments
                .Select(attachment => new MessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    FileName = attachment.FileName,
                    FileUrl = attachment.FileUrl,
                    ContentType = attachment.ContentType,
                    FileSizeBytes = attachment.FileSizeBytes,
                    CreatedAt = utcNow,
                    UpdatedAt = null,
                    IsDeleted = false,
                })
                .ToList();

            if (preparedAttachments.Count > 0)
            {
                await _attachmentRepository.AddRangeAsync(preparedAttachments, ct);
            }
        }

        var participants = await _participantRepository.GetParticipantsAsync(conversationId, ct);
        var recipientIds = participants
            .Select(p => p.Id)
            .Where(id => id != senderId)
            .Distinct()
            .ToList();

        if (recipientIds.Count > 0)
        {
            var receipts = recipientIds
                .Select(userId => new MessageReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    UserId = userId,
                    Status = MessageStatus.Sent,
                    CreatedAt = utcNow,
                    UpdatedAt = null,
                    IsDeleted = false,
                })
                .ToList();

            await _receiptRepository.AddRangeAsync(receipts, ct);
        }

        conversation.UpdatedAt = utcNow;
        _conversationRepository.Update(conversation);

        await _unitOfWork.SaveChangesAsync(ct);

        if (recipientIds.Count > 0)
        {
            await _notificationService.NotifyMessageSentAsync(message, recipientIds, ct);
        }

        return message;
    }

    public async Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    )
    {
        var isParticipant = await _conversationRepository.UserIsInConversationAsync(
            conversationId,
            readerUserId,
            ct
        );
        if (!isParticipant)
        {
            throw new InvalidOperationException("User does not belong to the conversation.");
        }

        var unreadIds = await _receiptRepository.GetUnreadMessageIdsForUserAsync(
            conversationId,
            readerUserId,
            ct
        );
        if (unreadIds.Count == 0)
        {
            return;
        }

        foreach (var messageId in unreadIds)
        {
            await _receiptRepository.MarkAsReadAsync(messageId, readerUserId, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _notificationService.NotifyMessagesReadAsync(
            conversationId,
            readerUserId,
            unreadIds,
            ct
        );
    }
}
