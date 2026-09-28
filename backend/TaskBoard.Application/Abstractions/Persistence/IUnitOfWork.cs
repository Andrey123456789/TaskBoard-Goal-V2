namespace TaskBoard.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Commits all staged changes atomically.
    /// </summary>
    /// <exception cref="UniqueConstraintViolationException">
    /// A staged change violates a unique constraint (for example a concurrent duplicate insert).
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
