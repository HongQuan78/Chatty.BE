using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
    Task<IReadOnlyList<User>> SearchUsersAsync(string keyword, CancellationToken ct = default);
    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);
    Task<bool> IsUserNameTakenAsync(string username, CancellationToken ct = default);
}
