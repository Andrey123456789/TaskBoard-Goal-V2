using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Seeding;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Keeps the Development seed dataset valid for the current schema and representative of the
/// specification's minimum demo data. Runs the seeder against its own empty database; no other
/// test depends on seed data.
/// </summary>
[TestFixture]
public sealed class DevelopmentSeedTests
{
    private TestDatabase _database = null!;
    private AppDbContext _db = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_database.ConnectionString).Options);

        Assert.That(await DbSeeder.SeedAsync(_db, CancellationToken.None), Is.True);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _db.DisposeAsync();
        await _database.DropAsync();
    }

    [Test]
    public async Task Seed_ContainsTheSpecifiedMinimumDemoData()
    {
        var ordinaryUsers = await _db.Users.CountAsync(x => x.Id != User.DeletedUserId);
        var projects = await _db.Projects.CountAsync();
        var tasks = await _db.Tasks.AsNoTracking().ToListAsync();
        var relations = await _db.TaskRelations.AsNoTracking().ToListAsync();

        int CountIn(TaskItemStatus status) => tasks.Count(x => x.Status == status);

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryUsers, Is.GreaterThanOrEqualTo(3));
            Assert.That(projects, Is.GreaterThanOrEqualTo(2));
            Assert.That(tasks.Count(x => x.AssigneeId is null), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountIn(TaskItemStatus.Created), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountIn(TaskItemStatus.InProgress), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountIn(TaskItemStatus.Completed), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountIn(TaskItemStatus.Closed), Is.GreaterThanOrEqualTo(1));
            Assert.That(tasks.Select(x => x.ProjectId).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(tasks.Where(x => x.AssigneeId is not null).Select(x => x.AssigneeId).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(relations, Is.Not.Empty);
        });

        var completedIds = tasks.Where(x => x.Status == TaskItemStatus.Completed).Select(x => x.Id).ToHashSet();
        var createdIds = tasks.Where(x => x.Status == TaskItemStatus.Created).Select(x => x.Id).ToHashSet();
        var hasFollowUp = relations.Any(x =>
            (completedIds.Contains(x.FirstTaskId) && createdIds.Contains(x.SecondTaskId))
            || (completedIds.Contains(x.SecondTaskId) && createdIds.Contains(x.FirstTaskId)));
        Assert.That(hasFollowUp, Is.True, "A completed task must have a related follow-up task.");
    }

    [Test]
    public async Task Seed_RespectsWorkflowInvariants()
    {
        var tasks = await _db.Tasks.AsNoTracking().ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                tasks.Where(x => x.Status != TaskItemStatus.Created).Select(x => x.AssigneeId),
                Has.None.Null,
                "Started tasks must have an assignee.");
            Assert.That(tasks.Select(x => x.AssigneeId), Has.None.EqualTo(User.DeletedUserId));
            Assert.That(tasks.All(x => x.UpdatedAt >= x.CreatedAt), Is.True, "UpdatedAt must not precede CreatedAt.");
        });
    }

    [Test]
    public async Task Seed_OnDatabaseWithData_LeavesItUnchanged()
    {
        var taskCount = await _db.Tasks.CountAsync();

        var seeded = await DbSeeder.SeedAsync(_db, CancellationToken.None);

        Assert.That(seeded, Is.False);
        Assert.That(await _db.Tasks.CountAsync(), Is.EqualTo(taskCount));
    }
}
