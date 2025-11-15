using System;
using System.Threading;
using System.Threading.Tasks;
using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories
{
    public class UserRepository(ChatDbContext context) : IUserRepository
    {
        private readonly ChatDbContext _context = context;

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        public Task AddAsync(User user, CancellationToken ct = default) =>
            _context.Users.AddAsync(user, ct).AsTask();
    }
}
