namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Raised by the persistence layer when committing changes violates a unique constraint.
/// Lets Application services translate a lost race against a concurrent duplicate into
/// the same outcome as the normal duplicate pre-check.
/// </summary>
public sealed class UniqueConstraintViolationException(Exception innerException)
    : Exception("A unique constraint was violated while saving changes.", innerException);
