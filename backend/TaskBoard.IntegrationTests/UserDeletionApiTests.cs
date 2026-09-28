using System.Net;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class UserDeletionApiTests : ApiTestBase
{
    private static readonly AssigneeDto DeletedUserAssignee = new(DeletedUserId, "Deleted User", IsDeletedUser: true);

    [Test]
    public async Task DeleteUser_WithActiveAndHistoricalTasks_ReassignsAllToDeletedUserWithoutDeletingTasks()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var created = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.Created);
        var inProgress = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.InProgress);
        var completed = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.Completed);
        var closed = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.Closed);
        var unassigned = await CreateTaskAsync(project.Id);

        var response = await Client.DeleteAsync($"/api/users/{user.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var tasks = await ListTasksAsync($"?projectId={project.Id}");
        Assert.That(tasks, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            foreach (var original in new[] { created, inProgress, completed, closed })
            {
                var task = tasks.Single(x => x.Id == original.Id);
                Assert.That(task.Assignee, Is.EqualTo(DeletedUserAssignee), original.Status.ToString());
                Assert.That(task.Status, Is.EqualTo(original.Status));
            }

            Assert.That(tasks.Single(x => x.Id == unassigned.Id).Assignee, Is.Null);
        });
    }

    [Test]
    public async Task DeletedAssigneeOfCreatedTask_TaskCannotStartUntilReassigned()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var replacement = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);
        await Client.DeleteAsync($"/api/users/{user.Id}");

        Assert.That((await GetTaskAsync(task.Id)).Assignee, Is.EqualTo(DeletedUserAssignee));
        await AssertProblemAsync(await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress), HttpStatusCode.Conflict);

        Assert.That(
            (await UpdateTaskAsync(task.Id, task.Title, task.Description, replacement.Id)).StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(
            (await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress)).StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DeletedAssigneeOfInProgressTask_TaskStaysInProgressAndCanBeReassigned()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var replacement = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.InProgress);

        await Client.DeleteAsync($"/api/users/{user.Id}");

        var afterDeletion = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(afterDeletion.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(afterDeletion.Assignee, Is.EqualTo(DeletedUserAssignee));
        });

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, replacement.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Assignee?.Id, Is.EqualTo(replacement.Id));
    }

    [Test]
    public async Task DeletedAssigneeOfCompletedTask_OnlyAssigneeAndUpdatedAtChange()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.Completed);
        var deletionTime = AdvanceClock();

        await Client.DeleteAsync($"/api/users/{user.Id}");

        var historical = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(historical.Assignee, Is.EqualTo(DeletedUserAssignee));
            Assert.That(historical.Status, Is.EqualTo(TaskItemStatus.Completed));
            Assert.That(historical.Title, Is.EqualTo(task.Title));
            Assert.That(historical.Description, Is.EqualTo(task.Description));
            Assert.That(historical.Project, Is.EqualTo(task.Project));
            Assert.That(historical.CreatedAt, Is.EqualTo(task.CreatedAt));
            Assert.That(historical.UpdatedAt, Is.EqualTo(deletionTime));
        });
    }
}
