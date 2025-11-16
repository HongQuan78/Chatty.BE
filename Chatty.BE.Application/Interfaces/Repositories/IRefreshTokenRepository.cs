using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<IReadOnlyList<RefreshToken>> GetTokensByUserIdAsync(
        Guid userId,
        bool includeRevoked,
        CancellationToken ct = default
    );
}
