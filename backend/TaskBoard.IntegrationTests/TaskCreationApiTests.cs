using System.Net;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class TaskCreationApiTests : ApiTestBase
{
    [Test]
    public async Task CreateTask_Unassigned_StartsInCreatedWithoutAssignee()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "Unassigned task", projectId = project.Id });

        var task = await ReadCreatedAsync<TaskDetailsDto>(response);
        Assert.Multiple(() =>
        {
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(task.Title, Is.EqualTo("Unassigned task"));
            Assert.That(task.Description, Is.Null);
            Assert.That(task.Assignee, Is.Null);
            Assert.That(task.Project, Is.EqualTo(new ProjectSummaryDto(project.Id, project.Name)));
            Assert.That(task.RelatedTasks, Is.Empty);
            Assert.That(task.CreatedAt, Is.EqualTo(Clock.GetUtcNow()));
            Assert.That(task.UpdatedAt, Is.EqualTo(task.CreatedAt));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/tasks/{task.Id}"));
        });
    }

    [Test]
    public async Task CreateTask_AssignedToUser_ReturnsAssigneeSummary()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();

        var task = await CreateTaskAsync(project.Id, user.Id);

        Assert.That(task.Assignee, Is.EqualTo(new AssigneeDto(user.Id, user.Name, IsDeletedUser: false)));
    }

    [Test]
    public async Task CreateTask_WithRelatedTaskIds_CreatesSymmetricRelationshipsOnce()
    {
        var project = await CreateProjectAsync();
        var first = await CreateTaskAsync(project.Id);
        var second = await CreateTaskAsync(project.Id);

        var task = await CreateTaskAsync(project.Id, relatedTaskIds: [first.Id, second.Id, first.Id]);

        Assert.That(task.RelatedTasks.Select(x => x.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
        Assert.That((await GetTaskAsync(first.Id)).RelatedTasks.Select(x => x.Id), Is.EquivalentTo(new[] { task.Id }));
        Assert.That((await GetTaskAsync(second.Id)).RelatedTasks.Select(x => x.Id), Is.EquivalentTo(new[] { task.Id }));
    }

    [Test]
    public async Task CreateTask_WithRelatedTaskInAnotherProject_IsAllowed()
    {
        var otherProject = await CreateProjectAsync();
        var otherTask = await CreateTaskAsync(otherProject.Id);
        var project = await CreateProjectAsync();

        var task = await CreateTaskAsync(project.Id, relatedTaskIds: [otherTask.Id]);

        Assert.That(task.RelatedTasks.Single().Project.Id, Is.EqualTo(otherProject.Id));
    }

    [Test]
    public async Task CreateTask_IgnoresClientSuppliedStatus()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "T", projectId = project.Id, status = "Completed" });

        Assert.That((await ReadCreatedAsync<TaskDetailsDto>(response)).Status, Is.EqualTo(TaskItemStatus.Created));
    }

    [Test]
    public async Task CreateTask_WithNonExistentProject_ReturnsUnprocessableContent()
    {
        var response = await PostTaskAsync(new { title = "T", projectId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateTask_WithNonExistentAssignee_ReturnsUnprocessableContent()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "T", projectId = project.Id, assigneeId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateTask_AssignedToDeletedUser_ReturnsUnprocessableContent()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "T", projectId = project.Id, assigneeId = DeletedUserId });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateTask_WithNonExistentRelatedTask_ReturnsUnprocessableContentAndCreatesNothing()
    {
        var project = await CreateProjectAsync();
        var existing = await CreateTaskAsync(project.Id);

        var response = await PostTaskAsync(new
        {
            title = "T",
            projectId = project.Id,
            relatedTaskIds = new[] { existing.Id, Guid.NewGuid() },
        });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.That(await ListTasksAsync($"?projectId={project.Id}"), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateTask_WithMissingTitleOrProject_ReturnsBadRequest()
    {
        await AssertProblemAsync(await PostTaskAsync(new { title = "", projectId = Guid.NewGuid() }), HttpStatusCode.BadRequest);
        await AssertProblemAsync(await PostTaskAsync(new { title = "T" }), HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTask_WithTitleOverMaximumLength_ReturnsBadRequest()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = new string('t', 201), projectId = project.Id });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetTask_WithUnknownId_ReturnsNotFound()
    {
        await AssertProblemAsync(await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }
}
