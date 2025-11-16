using System.Linq.Expressions;
using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly ChatDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(ChatDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    // READ
    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await _dbSet.Where(predicate).ToListAsync(ct);
    }

    public virtual Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return _dbSet.FirstOrDefaultAsync(predicate, ct);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, ct);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }

    // WRITE
    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, ct);
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _dbSet.UpdateRange(entities);
    }

    // SOFT DELETE
    public virtual Task SoftDeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.IsDeleted = true;
        _dbSet.Update(entity);

        return Task.CompletedTask;
    }

    public virtual Task SoftDeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
        }

        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    // HARD DELETE
    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
    }
}
