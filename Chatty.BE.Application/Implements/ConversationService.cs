using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Implements;

public class ConversationService(
    IConversationRepository conversationRepository,
    IConversationParticipantRepository participantRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork
) : IConversationService
{
    private readonly IConversationRepository _conversationRepository = conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository =
        participantRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<IReadOnlyList<Conversation>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) => _conversationRepository.GetConversationsOfUserAsync(userId, ct);

    public Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken ct = default) =>
        _conversationRepository.GetWithParticipantsAsync(conversationId, ct);

    public Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    ) => _conversationRepository.UserIsInConversationAsync(conversationId, userId, ct);

    public async Task<Conversation> CreatePrivateConversationAsync(
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

        var existing = await _conversationRepository.GetPrivateConversationAsync(
            userAId,
            userBId,
            ct
        );
        if (existing is not null)
        {
            return existing;
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

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _conversationRepository.AddAsync(conversation, ct);
            await _participantRepository.AddParticipantAsync(conversation.Id, userAId, ct);
            await _participantRepository.AddParticipantAsync(conversation.Id, userBId, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        await _notificationService.NotifyUserJoinedConversationAsync(conversation.Id, userAId, ct);
        await _notificationService.NotifyUserJoinedConversationAsync(conversation.Id, userBId, ct);

        return conversation;
    }

    public async Task<Conversation> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await EnsureUserExistsAsync(ownerId, ct);

        var distinctParticipantIds =
            participantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
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

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _conversationRepository.AddAsync(conversation, ct);

            foreach (var participantId in distinctParticipantIds)
            {
                await _participantRepository.AddParticipantAsync(
                    conversation.Id,
                    participantId,
                    ct
                );
            }

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        foreach (var participantId in distinctParticipantIds)
        {
            await _notificationService.NotifyUserJoinedConversationAsync(
                conversation.Id,
                participantId,
                ct
            );
        }

        return conversation;
    }

    public async Task AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await EnsureConversationExistsAsync(conversationId, ct);
        await EnsureUserExistsAsync(userId, ct);

        var alreadyParticipant = await _participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (alreadyParticipant)
        {
            return;
        }

        await _participantRepository.AddParticipantAsync(conversationId, userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _notificationService.NotifyUserJoinedConversationAsync(conversationId, userId, ct);
    }

    public async Task RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await EnsureConversationExistsAsync(conversationId, ct);

        var isParticipant = await _participantRepository.IsParticipantAsync(
            conversationId,
            userId,
            ct
        );
        if (!isParticipant)
        {
            return;
        }

        await _participantRepository.RemoveParticipantAsync(conversationId, userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _notificationService.NotifyUserLeftConversationAsync(conversationId, userId, ct);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        var exists = await _userRepository.ExistsAsync(userId, ct);
        if (!exists)
        {
            throw new KeyNotFoundException($"User {userId} was not found.");
        }
    }

    private async Task EnsureConversationExistsAsync(Guid conversationId, CancellationToken ct)
    {
        var exists = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (exists is null)
        {
            throw new KeyNotFoundException($"Conversation {conversationId} was not found.");
        }
    }
}
