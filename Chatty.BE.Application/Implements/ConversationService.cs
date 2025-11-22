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
    IObjectMapper objectMapper
) : IConversationService
{
    public async Task<IReadOnlyList<ConversationDto>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var conversations = await conversationRepository.GetConversationsOfUserAsync(userId, ct);
        return objectMapper.Map<List<ConversationDto>>(conversations);
    }

    public async Task<ConversationDto?> GetByIdAsync(
        Guid conversationId,
        CancellationToken ct = default
    )
    {
        var conversation = await conversationRepository.GetWithParticipantsAsync(
            conversationId,
            ct
        );
        return conversation is null ? null : objectMapper.Map<ConversationDto>(conversation);
    }

    public Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    ) => conversationRepository.UserIsInConversationAsync(conversationId, userId, ct);

    public async Task<ConversationDto> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    )
    {
        if (userAId == userBId)
        {
            throw new ArgumentException("Cannot create a private conversation with the same user.");
        }

        await EnsureUserExistsAsync(userAId, ct);
        await EnsureUserExistsAsync(userBId, ct);

        var existing = await conversationRepository.GetPrivateConversationAsync(
            userAId,
            userBId,
            ct
        );
        if (existing is not null)
        {
            return objectMapper.Map<ConversationDto>(existing);
        }

        var utcNow = DateTime.UtcNow;
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

        return objectMapper.Map<ConversationDto>(conversation);
    }

    public async Task<ConversationDto> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await EnsureUserExistsAsync(ownerId, ct);

        var distinctParticipantIds =
            participantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        if (!distinctParticipantIds.Contains(ownerId))
        {
            distinctParticipantIds.Add(ownerId);
        }

        foreach (var participantId in distinctParticipantIds)
        {
            await EnsureUserExistsAsync(participantId, ct);
        }

        var utcNow = DateTime.UtcNow;
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

        return objectMapper.Map<ConversationDto>(conversation);
    }

    public async Task AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await EnsureConversationExistsAsync(conversationId, ct);
        await EnsureUserExistsAsync(userId, ct);

        var alreadyParticipant = await participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (alreadyParticipant)
        {
            return;
        }

        await participantRepository.AddParticipantAsync(conversationId, userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await notificationService.NotifyUserJoinedConversationAsync(conversationId, userId, ct);
    }

    public async Task RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await EnsureConversationExistsAsync(conversationId, ct);

        var isParticipant = await participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (!isParticipant)
        {
            return;
        }

        await participantRepository.RemoveParticipantAsync(conversationId, userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await notificationService.NotifyUserLeftConversationAsync(conversationId, userId, ct);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        var exists = await userRepository.ExistsAsync(userId, ct);
        if (!exists)
        {
            throw new KeyNotFoundException($"User {userId} was not found.");
        }
    }

    private async Task EnsureConversationExistsAsync(Guid conversationId, CancellationToken ct)
    {
        var exists =
            await conversationRepository.GetByIdAsync(conversationId, ct)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} was not found.");
    }
}
