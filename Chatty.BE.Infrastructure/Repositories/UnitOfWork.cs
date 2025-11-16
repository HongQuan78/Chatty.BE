using Chatty.BE.Application.Interfaces;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Chatty.BE.Infrastructure.Repositories;

public class UnitOfWork(ChatDbContext context) : IUnitOfWork
{
    private readonly ChatDbContext _context = context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    // IDisposable
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _currentTransaction?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
