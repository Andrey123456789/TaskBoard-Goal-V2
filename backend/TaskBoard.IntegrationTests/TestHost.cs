using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Creates the isolated test database and the shared API host once per test run.
/// Tests create their own uniquely named data and never rely on Development seed data.
/// </summary>
[SetUpFixture]
public sealed class TestHost
{
    private static TestDatabase? _database;
    private static TaskBoardApiFactory? _factory;

    internal static TestDatabase Database =>
        _database ?? throw new InvalidOperationException("The test database is not initialized.");

    internal static TaskBoardApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("The test host is not initialized.");

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _factory = new TaskBoardApiFactory(_database.ConnectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DropAsync();
        }
    }
}
