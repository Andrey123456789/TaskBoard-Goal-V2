using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Infrastructure.Persistence.Seeding;

public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Populates an empty, fully migrated Development database with demo data. Never applies
    /// migrations and never changes existing data. Call only in the Development environment.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbSeeder).FullName!);

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            logger.LogWarning(
                "Development database has {PendingMigrationCount} pending migration(s); skipping development seeding. "
                + "Apply them with 'dotnet ef database update --project backend/TaskBoard.Infrastructure --startup-project backend/TaskBoard.Api'.",
                pendingMigrations.Count);

            return;
        }

        if (await DbSeeder.SeedAsync(db, cancellationToken))
        {
            logger.LogInformation("Development database seeded with demo data");
        }
    }
}
