using System.Net;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class UpdatedAtApiTests : ApiTestBase
{
    [Test]
    public async Task EditingCoreField_ChangesUpdatedAt()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        var editTime = AdvanceClock();

        await UpdateTaskAsync(task.Id, task.Title + " (edited)", task.Description, null);

        Assert.That((await GetTaskAsync(task.Id)).UpdatedAt, Is.EqualTo(editTime));
    }

    [Test]
    public async Task UpdateWithoutAnyChange_DoesNotChangeUpdatedAt()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        AdvanceClock();

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await GetTaskAsync(task.Id)).UpdatedAt, Is.EqualTo(task.UpdatedAt));
    }

    [Test]
    public async Task AllowedStatusTransition_ChangesUpdatedAt()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);
        var transitionTime = AdvanceClock();

        await ChangeStatusAsync(task.Id, TaskItemStatus.InProgress);

        Assert.That((await GetTaskAsync(task.Id)).UpdatedAt, Is.EqualTo(transitionTime));
    }

    [Test]
    public async Task DeletingCurrentAssignee_ChangesUpdatedAt()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);
        var deletionTime = AdvanceClock();

        await Client.DeleteAsync($"/api/users/{user.Id}");

        Assert.That((await GetTaskAsync(task.Id)).UpdatedAt, Is.EqualTo(deletionTime));
    }

    [Test]
    public async Task ChangingOnlyRelatedTasks_DoesNotChangeUpdatedAtOfEitherTask()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        var other = await CreateTaskAsync(project.Id);
        AdvanceClock();

        await ReplaceRelatedTasksAsync(task.Id, other.Id);
        AdvanceClock();
        await ReplaceRelatedTasksAsync(task.Id);

        Assert.That((await GetTaskAsync(task.Id)).UpdatedAt, Is.EqualTo(task.UpdatedAt));
        Assert.That((await GetTaskAsync(other.Id)).UpdatedAt, Is.EqualTo(other.UpdatedAt));
    }
}
