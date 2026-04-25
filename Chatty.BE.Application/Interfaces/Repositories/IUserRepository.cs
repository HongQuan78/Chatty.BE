using Chatty.BE.Application.Common;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    /// <summary>
    /// Searches for users using an optimized algorithm for large datasets.
    /// Supports pagination and prioritizing relevant results.
    /// </summary>
    Task<PagedList<User>> SearchUsersAsync(
        string keyword,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);
    Task<bool> IsUserNameTakenAsync(string username, CancellationToken ct = default);
}
