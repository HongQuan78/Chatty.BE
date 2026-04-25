using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Implements;

public class ConversationService(
    IConversationRepository conversationRepository,
    IConversationParticipantRepository participantRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IObjectMapper objectMapper,
    IDateTimeProvider dateTimeProvider
) : IConversationService
{
    public async Task<Result<IReadOnlyList<ConversationDto>>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var conversations = await conversationRepository.GetConversationsOfUserAsync(userId, ct);
        return Result<IReadOnlyList<ConversationDto>>.Success(objectMapper.Map<List<ConversationDto>>(conversations));
    }

    public async Task<Result<ConversationDto>> GetByIdAsync(
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
            return Result<ConversationDto>.Failure("User is not a member of the conversation.", "FORBIDDEN");
        }

        var conversation = await conversationRepository.GetWithParticipantsAsync(
            conversationId,
            ct
        );
        if (conversation is null)
        {
            return Result<ConversationDto>.Failure("Conversation was not found.", "NOT_FOUND");
        }

        return Result<ConversationDto>.Success(objectMapper.Map<ConversationDto>(conversation));
    }

    public async Task<Result<bool>> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var isIn = await conversationRepository.UserIsInConversationAsync(conversationId, userId, ct);
        return Result<bool>.Success(isIn);
    }

    public async Task<Result<ConversationDto>> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    )
    {
        if (userAId == userBId)
        {
            return Result<ConversationDto>.Failure("Cannot create a private conversation with the same user.", "BAD_REQUEST");
        }

        var userAExists = await userRepository.ExistsAsync(userAId, ct);
        if (!userAExists) return Result<ConversationDto>.Failure($"User {userAId} was not found.", "NOT_FOUND");

        var userBExists = await userRepository.ExistsAsync(userBId, ct);
        if (!userBExists) return Result<ConversationDto>.Failure($"User {userBId} was not found.", "NOT_FOUND");

        var existing = await conversationRepository.GetPrivateConversationAsync(
            userAId,
            userBId,
            ct
        );
        if (existing is not null)
        {
            return Result<ConversationDto>.Success(objectMapper.Map<ConversationDto>(existing));
        }

        var utcNow = dateTimeProvider.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = null,
            IsGroup = false,
            OwnerId = null,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await conversationRepository.AddAsync(conversation, ct);
            await participantRepository.AddParticipantAsync(conversation.Id, userAId, ct);
            await participantRepository.AddParticipantAsync(conversation.Id, userBId, ct);

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        await notificationService.NotifyUserJoinedConversationAsync(conversation.Id, userAId, ct);
        await notificationService.NotifyUserJoinedConversationAsync(conversation.Id, userBId, ct);

        return Result<ConversationDto>.Success(objectMapper.Map<ConversationDto>(conversation));
    }

    public async Task<Result<ConversationDto>> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ConversationDto>.Failure("Group name is required.", "BAD_REQUEST");
        }

        var ownerExists = await userRepository.ExistsAsync(ownerId, ct);
        if (!ownerExists) return Result<ConversationDto>.Failure($"Owner {ownerId} was not found.", "NOT_FOUND");

        var distinctParticipantIds =
            participantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        if (!distinctParticipantIds.Contains(ownerId))
        {
            distinctParticipantIds.Add(ownerId);
        }

        foreach (var participantId in distinctParticipantIds)
        {
            var exists = await userRepository.ExistsAsync(participantId, ct);
            if (!exists) return Result<ConversationDto>.Failure($"Participant {participantId} was not found.", "NOT_FOUND");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsGroup = true,
            OwnerId = ownerId,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await conversationRepository.AddAsync(conversation, ct);

            foreach (var participantId in distinctParticipantIds)
            {
                await participantRepository.AddParticipantAsync(conversation.Id, participantId, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        foreach (var participantId in distinctParticipantIds)
        {
            await notificationService.NotifyUserJoinedConversationAsync(
                conversation.Id,
                participantId,
                ct
            );
        }

        return Result<ConversationDto>.Success(objectMapper.Map<ConversationDto>(conversation));
    }

    public async Task<Result> AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    )
    {
        var conversation = await conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result.Failure("Conversation not found.", "NOT_FOUND");

        if (conversation.IsGroup && conversation.OwnerId != actorId)
        {
            return Result.Failure("Only the owner can add participants to this group.", "FORBIDDEN");
        }

        var userExists = await userRepository.ExistsAsync(userId, ct);
        if (!userExists) return Result.Failure("User not found.", "NOT_FOUND");

        var alreadyParticipant = await participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (alreadyParticipant)
        {
            return Result.Success();
        }

        await participantRepository.AddParticipantAsync(conversationId, userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await notificationService.NotifyUserJoinedConversationAsync(conversationId, userId, ct);
        return Result.Success();
    }

    public async Task<Result> RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    )
    {
        var conversation = await conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null) return Result.Failure("Conversation not found.", "NOT_FOUND");

        // Allow owner to remove anyone, or any user to remove themselves.
        if (conversation.IsGroup && conversation.OwnerId != actorId && userId != actorId)
        {
            return Result.Failure("You don't have permission to remove this participant.", "FORBIDDEN");
        }

        var isParticipant = await participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (!isParticipant)
        {
            return Result.Success();
        }

        await participantRepository.RemoveParticipantAsync(conversationId, userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await notificationService.NotifyUserLeftConversationAsync(conversationId, userId, ct);
        return Result.Success();
    }
}
