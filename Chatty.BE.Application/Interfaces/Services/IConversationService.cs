using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IConversationService
{
    /// <summary>
    /// Tạo private conversation giữa 2 user.
    /// Nếu đã tồn tại thì có thể return lại conversation cũ (tùy implement).
    /// </summary>
    Task<Conversation> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Tạo group conversation với owner + danh sách participant ban đầu.
    /// </summary>
    Task<Conversation> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    );

    /// <summary>
    /// Lấy danh sách conversation (private + group) mà user tham gia.
    /// </summary>
    Task<IReadOnlyList<Conversation>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Lấy chi tiết conversation kèm theo participants (nếu implement).
    /// </summary>
    Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken ct = default);

    Task AddParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task RemoveParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}
