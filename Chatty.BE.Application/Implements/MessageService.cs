using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.Extensions;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using FluentValidation;

namespace Chatty.BE.Application.Implements;

public class MessageService(
    IMessageRepository messageRepository,
    IMessageAttachmentRepository attachmentRepository,
    IMessageReceiptRepository receiptRepository,
    IConversationRepository conversationRepository,
    IConversationParticipantRepository participantRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IObjectMapper mapper,
    IDateTimeProvider dateTimeProvider,
    IValidator<SendMessageRequest> validator
) : IMessageService
{
    public async Task<Result<IReadOnlyList<MessageDto>>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var isParticipant = await conversationRepository.UserIsInConversationAsync(
            conversationId,
            userId,
            ct
        );
        if (!isParticipant)
        {
            return Result<IReadOnlyList<MessageDto>>.Failure("User is not a member of the conversation.", "FORBIDDEN");
        }

        var messageList = await messageRepository.GetMessagesAsync(
            conversationId,
            page,
            pageSize,
            ct
        );
        return Result<IReadOnlyList<MessageDto>>.Success(mapper.Map<IReadOnlyList<MessageDto>>(messageList));
    }

    public async Task<Result<int>> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var isParticipant = await conversationRepository.UserIsInConversationAsync(
            conversationId,
            userId,
            ct
        );
        if (!isParticipant)
        {
            return Result<int>.Failure("User is not a member of the conversation.", "FORBIDDEN");
        }

        var count = await messageRepository.CountUnreadMessagesAsync(conversationId, userId, ct);
        return Result<int>.Success(count);
    }

    public async Task<Result<MessageDto>> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<MessageDto>();
        }

        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, ct);
        if (conversation is null)
        {
            return Result<MessageDto>.Failure($"Conversation {request.ConversationId} was not found.", "NOT_FOUND");
        }

        var isParticipant = await conversationRepository.UserIsInConversationAsync(
            request.ConversationId,
            request.SenderId,
            ct
        );
        if (!isParticipant)
        {
            return Result<MessageDto>.Failure("Sender is not a member of the conversation.", "FORBIDDEN");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            Type = request.Type,
            Status = MessageStatus.Sent,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await messageRepository.AddAsync(message, ct);

        if (request.Attachments is not null && request.Attachments.Count > 0)
        {
            var preparedAttachments = request.Attachments
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

            await attachmentRepository.AddRangeAsync(preparedAttachments, ct);
        }

        var participants = await participantRepository.GetParticipantsAsync(request.ConversationId, ct);
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

        return Result<MessageDto>.Success(mapper.Map<MessageDto>(message));
    }

    public async Task<Result> MarkConversationAsReadAsync(
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
            return Result.Failure("User does not belong to the conversation.", "FORBIDDEN");
        }

        var unreadIds = await receiptRepository.GetUnreadMessageIdsForUserAsync(
            conversationId,
            readerUserId,
            ct
        );
        if (unreadIds.Count == 0)
        {
            return Result.Success();
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

        return Result.Success();
    }
}
