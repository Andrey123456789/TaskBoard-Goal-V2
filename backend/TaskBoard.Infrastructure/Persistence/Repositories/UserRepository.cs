using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<IReadOnlyList<User>> ListOrdinaryAsync(CancellationToken cancellationToken) =>
        await db.Users
            .AsNoTracking()
            .Where(x => x.Id != User.DeletedUserId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.Id == id, cancellationToken);

    // The Email column uses a case-insensitive collation, so this comparison is case-insensitive.
    public Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.Email == email && x.Id != excludedUserId, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
