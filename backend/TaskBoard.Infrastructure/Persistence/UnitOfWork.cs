using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;

namespace TaskBoard.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // SQL Server error numbers for duplicate keys in a unique index / unique constraint.
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInUniqueConstraint = 2627;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        {
            Number: DuplicateKeyInUniqueIndex or DuplicateKeyInUniqueConstraint,
        })
        {
            throw new UniqueConstraintViolationException(exception);
        }
    }
}
