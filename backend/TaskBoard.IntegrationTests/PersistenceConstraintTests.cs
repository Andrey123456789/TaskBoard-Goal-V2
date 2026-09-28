using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Verifies the database-level guarantees behind the API rules (they also protect against
/// concurrent requests that pass the Application pre-checks) on the real SQL Server provider.
/// </summary>
[TestFixture]
public sealed class PersistenceConstraintTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private AppDbContext _db = null!;

    [SetUp]
    public void SetUp() =>
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(TestHost.Database.ConnectionString).Options);

    [TearDown]
    public async Task TearDownAsync() => await _db.DisposeAsync();

    [Test]
    public void SaveChanges_WithEmailsDifferingOnlyInCase_ThrowsUniqueConstraintViolation()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        _db.Users.AddRange(NewUser(email), NewUser(email.ToUpperInvariant()));

        Assert.That(
            async () => await new UnitOfWork(_db).SaveChangesAsync(),
            Throws.TypeOf<UniqueConstraintViolationException>());
    }

    [Test]
    public void SaveChanges_WithDuplicateProjectName_ThrowsUniqueConstraintViolation()
    {
        var name = $"Project-{Guid.NewGuid():N}";
        _db.Projects.AddRange(NewProject(name), NewProject(name));

        Assert.That(
            async () => await new UnitOfWork(_db).SaveChangesAsync(),
            Throws.TypeOf<UniqueConstraintViolationException>());
    }

    [Test]
    public async Task SaveChanges_WithProjectNamesDifferingOnlyInCase_Succeeds()
    {
        var name = $"Project-{Guid.NewGuid():N}";
        _db.Projects.AddRange(NewProject(name), NewProject(name.ToUpperInvariant()));

        Assert.That(await new UnitOfWork(_db).SaveChangesAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task SaveChanges_WithSelfRelation_IsRejectedByCheckConstraint()
    {
        var project = NewProject($"Project-{Guid.NewGuid():N}");
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            ProjectId = project.Id,
            Status = TaskItemStatus.Created,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        _db.Projects.Add(project);
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        _db.TaskRelations.Add(new TaskRelation { FirstTaskId = task.Id, SecondTaskId = task.Id });

        Assert.That(async () => await _db.SaveChangesAsync(), Throws.InstanceOf<DbUpdateException>());
    }

    private static User NewUser(string email) =>
        new() { Id = Guid.NewGuid(), Name = "User", Email = email, CreatedAt = Now };

    private static Project NewProject(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, CreatedAt = Now };
}
