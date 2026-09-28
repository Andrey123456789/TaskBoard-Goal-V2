using System.Net;
using System.Net.Http.Json;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class TaskWorkflowApiTests : ApiTestBase
{
    [Test]
    public async Task Start_UnassignedTask_ReturnsConflict()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress), HttpStatusCode.Conflict);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.Created));
    }

    [Test]
    public async Task Start_TaskAssignedToDeletedUser_ReturnsConflict()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);
        await Client.DeleteAsync($"/api/users/{user.Id}");

        await AssertProblemAsync(await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress), HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Start_TaskAssignedToActiveUser_MovesToInProgress()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);

        var response = await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.InProgress));
    }

    [Test]
    public async Task InProgressTask_CanBeReassignedToAnotherActiveUser()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var otherUser = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.InProgress);

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, otherUser.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Assignee?.Id, Is.EqualTo(otherUser.Id));
    }

    [Test]
    public async Task InProgressTask_CannotBeManuallyUnassigned()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.InProgress);

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, assigneeId: null);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.That((await GetTaskAsync(task.Id)).Assignee?.Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task InProgressTask_CannotBeManuallyAssignedToDeletedUser()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.InProgress);

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, DeletedUserId);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task FullForwardWorkflow_CreatedToClosed_Succeeds()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);

        foreach (var status in new[] { TaskItemStatus.InProgress, TaskItemStatus.Completed, TaskItemStatus.Closed })
        {
            var response = await ChangeStatusAsync(task.Id, status);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"Transition to {status}");
            Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(status));
        }
    }

    [TestCase(TaskItemStatus.Created, TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Created, TaskItemStatus.Closed)]
    [TestCase(TaskItemStatus.Created, TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Closed)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Closed)]
    public async Task InvalidTransition_ReturnsConflictAndKeepsStatus(TaskItemStatus current, TaskItemStatus target)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, current);

        await AssertProblemAsync(await ChangeStatusAsync(task.Id, target), HttpStatusCode.Conflict);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(current));
    }

    [Test]
    public async Task ChangeStatus_WithMissingOrUnknownStatus_ReturnsBadRequest()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(
            await Client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { }),
            HttpStatusCode.BadRequest);
        await AssertProblemAsync(
            await Client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Reopened" }),
            HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ChangeStatus_OfUnknownTask_ReturnsNotFound()
    {
        await AssertProblemAsync(await ChangeStatusAsync(Guid.NewGuid(), TaskItemStatus.InProgress), HttpStatusCode.NotFound);
    }
}
