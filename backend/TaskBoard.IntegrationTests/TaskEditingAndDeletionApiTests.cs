using System.Net;
using System.Net.Http.Json;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class TaskEditingAndDeletionApiTests : ApiTestBase
{
    [Test]
    public async Task UpdateCreatedTask_ChangesTitleDescriptionAndAssignee()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await UpdateTaskAsync(task.Id, "New title", "New description", user.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var updated = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Title, Is.EqualTo("New title"));
            Assert.That(updated.Description, Is.EqualTo("New description"));
            Assert.That(updated.Assignee?.Id, Is.EqualTo(user.Id));
            Assert.That(updated.Status, Is.EqualTo(TaskItemStatus.Created));
        });
    }

    [Test]
    public async Task UpdateCreatedTask_CanUnassign()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, assigneeId: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Assignee, Is.Null);
    }

    [Test]
    public async Task UpdateTask_CannotMoveTaskToAnotherProject()
    {
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new { title = "Renamed", description = (string?)null, assigneeId = (Guid?)null, projectId = otherProject.Id });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Project.Id, Is.EqualTo(project.Id));
    }

    [Test]
    public async Task UpdateTask_WithNonExistentAssignee_ReturnsUnprocessableContent()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(
            await UpdateTaskAsync(task.Id, task.Title, null, Guid.NewGuid()),
            HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task UpdateTask_ManuallyAssigningDeletedUser_ReturnsUnprocessableContent()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(
            await UpdateTaskAsync(task.Id, task.Title, null, DeletedUserId),
            HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task UpdateTask_KeepingSystemAssignedDeletedUser_IsAllowed()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);
        await Client.DeleteAsync($"/api/users/{user.Id}");

        var response = await UpdateTaskAsync(task.Id, "Edited while assigned to Deleted User", null, DeletedUserId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).Assignee?.Id, Is.EqualTo(DeletedUserId));
    }

    [Test]
    public async Task UpdateTask_WithEmptyTitle_ReturnsBadRequest()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(await UpdateTaskAsync(task.Id, "", null, null), HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTask_OfUnknownTask_ReturnsNotFound()
    {
        await AssertProblemAsync(await UpdateTaskAsync(Guid.NewGuid(), "T", null, null), HttpStatusCode.NotFound);
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task UpdateTask_WhenCompletedOrClosed_ReturnsConflictAndKeepsCoreData(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var otherUser = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);

        var response = await UpdateTaskAsync(task.Id, "Changed", "Changed", otherUser.Id);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        var unchanged = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Title, Is.EqualTo(task.Title));
            Assert.That(unchanged.Description, Is.EqualTo(task.Description));
            Assert.That(unchanged.Assignee?.Id, Is.EqualTo(user.Id));
            Assert.That(unchanged.UpdatedAt, Is.EqualTo(task.UpdatedAt));
        });
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task DeleteTask_WhenCompletedOrClosed_ReturnsConflict(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);

        await AssertProblemAsync(await Client.DeleteAsync($"/api/tasks/{task.Id}"), HttpStatusCode.Conflict);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(status));
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    public async Task DeleteTask_WhenCreatedOrInProgress_RemovesTaskAndItsRelationships(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);
        var related = await CreateTaskAsync(project.Id, relatedTaskIds: [task.Id]);

        var response = await Client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertProblemAsync(await Client.GetAsync($"/api/tasks/{task.Id}"), HttpStatusCode.NotFound);
        Assert.That((await GetTaskAsync(related.Id)).RelatedTasks, Is.Empty);
    }

    [Test]
    public async Task DeleteTask_OfUnknownTask_ReturnsNotFound()
    {
        await AssertProblemAsync(await Client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }
}
