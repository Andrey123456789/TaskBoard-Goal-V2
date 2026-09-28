using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Support;

/// <summary>
/// An isolated SQL Server database created (and migrated) for one test run and dropped
/// afterwards. Uses LocalDB unless <c>TASKBOARD_TESTS_SQLSERVER</c> supplies another server
/// connection string. Never points at the Development database.
/// </summary>
internal sealed class TestDatabase
{
    private const string ServerConnectionVariable = "TASKBOARD_TESTS_SQLSERVER";

    private const string DefaultServerConnection =
        "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private TestDatabase(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    /// <summary>
    /// A connection string for a database that is never created; used by hosts that must not
    /// touch any real database.
    /// </summary>
    public static string MissingDatabaseConnectionString() => BuildConnectionString($"TaskBoardTests_Missing_{Guid.NewGuid():N}");

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase(BuildConnectionString($"TaskBoardTests_{Guid.NewGuid():N}"));

        await using var db = database.CreateDbContext();
        await db.Database.MigrateAsync();

        return database;
    }

    public async Task DropAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    private static string BuildConnectionString(string databaseName)
    {
        var serverConnection = Environment.GetEnvironmentVariable(ServerConnectionVariable);

        return new SqlConnectionStringBuilder(
            string.IsNullOrWhiteSpace(serverConnection) ? DefaultServerConnection : serverConnection)
        {
            InitialCatalog = databaseName,
        }.ConnectionString;
    }
}
