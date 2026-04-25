using Chatty.BE.Application.Common;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class UserRepository(ChatDbContext context)
    : GenericRepository<User>(context),
        IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        return _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, ct);
    }

    public Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return _context.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);
    }

    public Task<bool> IsUserNameTakenAsync(string username, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return _context.Users.AsNoTracking().AnyAsync(u => u.UserName == username, ct);
    }

    public async Task<PagedList<User>> SearchUsersAsync(
        string keyword,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return PagedList<User>.Create([], 0, pageIndex, pageSize);
        }

        var searchTerm = keyword.Trim();
        var containsPattern = $"%{searchTerm}%";
        var startsWithPattern = $"{searchTerm}%";

        // 1. Base Query with Filtering
        var query = _context.Users.AsNoTracking()
            .Where(u =>
                EF.Functions.Like(u.UserName, containsPattern) ||
                EF.Functions.Like(u.Email, containsPattern) ||
                (u.DisplayName != null && EF.Functions.Like(u.DisplayName, containsPattern))
            );

        // 2. Optimized Sorting Algorithm (Relevance)
        // We prioritize:
        // - Exact matches (UserName or Email)
        // - StartsWith matches (UserName or DisplayName)
        // - Substring matches
        var prioritizedQuery = query.OrderByDescending(u =>
                (u.UserName == searchTerm || u.Email == searchTerm) ? 3 :
                (EF.Functions.Like(u.UserName, startsWithPattern) || (u.DisplayName != null && EF.Functions.Like(u.DisplayName, startsWithPattern))) ? 2 :
                1
            )
            .ThenBy(u => u.DisplayName ?? u.UserName);

        // 3. Execution with Keyset-style Paging
        var totalCount = await query.CountAsync(ct);
        var items = await prioritizedQuery
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedList<User>.Create(items, totalCount, pageIndex, pageSize);
    }
}
