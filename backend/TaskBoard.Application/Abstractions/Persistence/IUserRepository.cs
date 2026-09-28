using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IUserRepository
{
    /// <summary>
    /// Returns ordinary users (never the system <c>Deleted User</c>), ordered by name.
    /// </summary>
    Task<IReadOnlyList<User>> ListOrdinaryAsync(CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether another user already uses the email, compared case-insensitively.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
