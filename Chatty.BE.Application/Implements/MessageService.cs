using Chatty.BE.Application.DTOs.Messages;
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
    IUnitOfWork unitOfWork,
    IObjectMapper mapper
) : IMessageService
{
    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var messageList = await messageRepository.GetMessagesAsync(
            conversationId,
            page,
            pageSize,
            ct
        );
        return mapper.Map<IReadOnlyList<MessageDto>>(messageList);
    }

    public Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    ) => messageRepository.CountUnreadMessagesAsync(conversationId, userId, ct);

    public async Task<MessageDto> SendMessageAsync(
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
            await conversationRepository.GetByIdAsync(conversationId, ct)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} was not found.");

        var isParticipant = await conversationRepository.UserIsInConversationAsync(
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

        await messageRepository.AddAsync(message, ct);

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
                await attachmentRepository.AddRangeAsync(preparedAttachments, ct);
            }
        }

        var participants = await participantRepository.GetParticipantsAsync(conversationId, ct);
        // Notify all participants (including sender) so multiple sessions for the same user also update in real-time.
        var recipientIds = participants.Select(p => p.Id).Distinct().ToList();

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

            await receiptRepository.AddRangeAsync(receipts, ct);
        }

        conversation.UpdatedAt = utcNow;
        conversationRepository.Update(conversation);

        await unitOfWork.SaveChangesAsync(ct);

        if (recipientIds.Count > 0)
        {
            await notificationService.NotifyMessageSentAsync(message, recipientIds, ct);
        }

        return mapper.Map<MessageDto>(message);
    }

    public async Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    )
    {
        var isParticipant = await conversationRepository.UserIsInConversationAsync(
            conversationId,
            readerUserId,
            ct
        );
        if (!isParticipant)
        {
            throw new InvalidOperationException("User does not belong to the conversation.");
        }

        var unreadIds = await receiptRepository.GetUnreadMessageIdsForUserAsync(
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
            await receiptRepository.MarkAsReadAsync(messageId, readerUserId, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await notificationService.NotifyMessagesReadAsync(
            conversationId,
            readerUserId,
            unreadIds,
            ct
        );
    }
}
