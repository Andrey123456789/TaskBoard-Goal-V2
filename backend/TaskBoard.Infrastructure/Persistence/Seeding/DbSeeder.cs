using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development demo data. The system Deleted User is not demo data; it is created by the
/// migrations. Runs only against an empty, fully migrated Development database.
/// </summary>
internal static class DbSeeder
{
    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<bool> SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Projects.AnyAsync(cancellationToken))
        {
            return false;
        }

        var alice = CreateUser("11111111-1111-1111-1111-000000000001", "Alice Johnson", "alice.johnson@example.com", 0);
        var bob = CreateUser("11111111-1111-1111-1111-000000000002", "Bob Smith", "bob.smith@example.com", 0);
        var carol = CreateUser("11111111-1111-1111-1111-000000000003", "Carol Davis", "carol.davis@example.com", 0);

        var website = CreateProject(
            "22222222-2222-2222-2222-000000000001",
            "Website Redesign",
            "Refresh the public marketing website.");
        var mobile = CreateProject(
            "22222222-2222-2222-2222-000000000002",
            "Mobile App",
            description: null);

        var wireframes = CreateTask(
            "33333333-3333-3333-3333-000000000001", "Draft homepage wireframes", website, assignee: null,
            TaskItemStatus.Created, createdDay: 1, updatedDay: 1,
            "Low-fidelity wireframes for the new homepage layout.");
        var apiGuide = CreateTask(
            "33333333-3333-3333-3333-000000000002", "Write API integration guide", mobile, bob,
            TaskItemStatus.Created, createdDay: 2, updatedDay: 2);
        var navigation = CreateTask(
            "33333333-3333-3333-3333-000000000003", "Implement responsive navigation", website, alice,
            TaskItemStatus.InProgress, createdDay: 1, updatedDay: 3,
            "Collapsible menu for small screens.");
        var loginScreen = CreateTask(
            "33333333-3333-3333-3333-000000000004", "Build login screen", mobile, carol,
            TaskItemStatus.InProgress, createdDay: 2, updatedDay: 4);
        var ciPipeline = CreateTask(
            "33333333-3333-3333-3333-000000000005", "Set up CI pipeline", website, bob,
            TaskItemStatus.Completed, createdDay: 0, updatedDay: 5,
            "Build and test on every push.");
        var appIcon = CreateTask(
            "33333333-3333-3333-3333-000000000006", "Design app icon", mobile, alice,
            TaskItemStatus.Completed, createdDay: 0, updatedDay: 4);
        var colorPalette = CreateTask(
            "33333333-3333-3333-3333-000000000007", "Choose color palette", website, carol,
            TaskItemStatus.Closed, createdDay: 0, updatedDay: 2);
        var ciCaching = CreateTask(
            "33333333-3333-3333-3333-000000000008", "Add build caching to CI pipeline", website, alice,
            TaskItemStatus.Created, createdDay: 6, updatedDay: 6,
            "Follow-up to the completed CI pipeline task.");

        db.Users.AddRange(alice, bob, carol);
        db.Projects.AddRange(website, mobile);
        db.Tasks.AddRange(wireframes, apiGuide, navigation, loginScreen, ciPipeline, appIcon, colorPalette, ciCaching);
        db.TaskRelations.AddRange(
            TaskRelationKey.Create(navigation.Id, wireframes.Id),
            TaskRelationKey.Create(loginScreen.Id, apiGuide.Id),
            TaskRelationKey.Create(ciCaching.Id, ciPipeline.Id));

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static User CreateUser(string id, string name, string email, int createdDay) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        Email = email,
        CreatedAt = BaseTime.AddDays(createdDay),
    };

    private static Project CreateProject(string id, string name, string? description) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        Description = description,
        CreatedAt = BaseTime,
    };

    private static TaskItem CreateTask(
        string id,
        string title,
        Project project,
        User? assignee,
        TaskItemStatus status,
        int createdDay,
        int updatedDay,
        string? description = null) => new()
        {
            Id = Guid.Parse(id),
            Title = title,
            Description = description,
            ProjectId = project.Id,
            AssigneeId = assignee?.Id,
            Status = status,
            CreatedAt = BaseTime.AddDays(createdDay).AddHours(1),
            UpdatedAt = BaseTime.AddDays(updatedDay).AddHours(2),
        };
}
