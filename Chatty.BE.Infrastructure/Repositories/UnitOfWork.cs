using System;
using System.Threading;
using System.Threading.Tasks;
using Chatty.BE.Application.Interfaces;
using Chatty.BE.Infrastructure.Persistence;

namespace Chatty.BE.Infrastructure.Repositories
{
    public class UnitOfWork(ChatDbContext context) : IUnitOfWork
    {
        private readonly ChatDbContext _context = context;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    }
}
