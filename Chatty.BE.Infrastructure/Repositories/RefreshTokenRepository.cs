using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(ChatDbContext context)
    : GenericRepository<RefreshToken>(context),
        IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetTokensByUserIdAsync(
        Guid userId,
        bool includeRevoked,
        CancellationToken ct = default
    )
    {
        var query = _context.RefreshTokens.Where(t => t.UserId == userId);

        if (!includeRevoked)
        {
            query = query.Where(t => !t.RevokedAt.HasValue);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }
}
