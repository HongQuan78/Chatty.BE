using System.Linq.Expressions;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces;

public interface IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    // READ
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );

    // WRITE
    Task AddAsync(TEntity entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    // SOFT DELETE
    Task SoftDeleteAsync(TEntity entity, CancellationToken ct = default);

    Task SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // HARD DELETE
    void Remove(TEntity entity);
}
