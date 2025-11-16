using Chatty.BE.Application.Interfaces;
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

    public async Task<IReadOnlyList<User>> SearchUsersAsync(
        string keyword,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var pattern = $"%{keyword.Trim()}%";

        return await _context
            .Users.AsNoTracking()
            .Where(u =>
                EF.Functions.Like(u.UserName, pattern)
                || EF.Functions.Like(u.Email, pattern)
                || (u.DisplayName != null && EF.Functions.Like(u.DisplayName, pattern))
            )
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .ToListAsync(ct);
    }
}
