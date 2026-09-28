using System.Net;
using System.Net.Http.Json;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class RelatedTasksApiTests : ApiTestBase
{
    [Test]
    public async Task ReplaceRelatedTasks_FullReplaceSetLifecycle_ConvergesToRequestedSet()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        var a = await CreateTaskAsync(project.Id);
        var b = await CreateTaskAsync(project.Id);
        var c = await CreateTaskAsync(project.Id);
        var d = await CreateTaskAsync(project.Id);

        // Add several with one request.
        Assert.That((await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id, c.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id, a.Id, b.Id, c.Id);

        // Repeating the identical request is idempotent.
        Assert.That((await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id, c.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id, a.Id, b.Id, c.Id);

        // Removing one ID removes only that relationship, from both sides.
        await ReplaceRelatedTasksAsync(task.Id, a.Id, c.Id);
        await AssertRelatedAsync(task.Id, a.Id, c.Id);
        await AssertRelatedAsync(b.Id);

        // Adding another ID keeps the existing ones.
        await ReplaceRelatedTasksAsync(task.Id, a.Id, c.Id, d.Id);
        await AssertRelatedAsync(task.Id, a.Id, c.Id, d.Id);

        // An empty array removes all relationships.
        Assert.That((await ReplaceRelatedTasksAsync(task.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id);
        await AssertRelatedAsync(a.Id);
        await AssertRelatedAsync(d.Id);
    }

    [Test]
    public async Task ReplaceRelatedTasks_IsSymmetricFromBothTaskDetails()
    {
        var project = await CreateProjectAsync();
        var first = await CreateTaskAsync(project.Id);
        var second = await CreateTaskAsync(project.Id);

        await ReplaceRelatedTasksAsync(first.Id, second.Id);

        var secondDetails = await GetTaskAsync(second.Id);
        Assert.That(secondDetails.RelatedTasks.Single().Id, Is.EqualTo(first.Id));
        Assert.That(secondDetails.RelatedTasks.Single().Title, Is.EqualTo(first.Title));
        await AssertRelatedAsync(first.Id, second.Id);
    }

    [Test]
    public async Task ReplaceRelatedTasks_FromTheOtherSide_DoesNotDuplicateRelationship()
    {
        var project = await CreateProjectAsync();
        var first = await CreateTaskAsync(project.Id);
        var second = await CreateTaskAsync(project.Id);
        await ReplaceRelatedTasksAsync(first.Id, second.Id);

        var response = await ReplaceRelatedTasksAsync(second.Id, first.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(first.Id, second.Id);
        await AssertRelatedAsync(second.Id, first.Id);
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithDuplicateIds_CreatesOneLogicalRelationship()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        var other = await CreateTaskAsync(project.Id);

        var response = await ReplaceRelatedTasksAsync(task.Id, other.Id, other.Id, other.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id, other.Id);
        await AssertRelatedAsync(other.Id, task.Id);
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithSelfReference_ReturnsUnprocessableContent()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(await ReplaceRelatedTasksAsync(task.Id, task.Id), HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithNonExistentTask_ReturnsUnprocessableContentAndKeepsCurrentSet()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);
        var other = await CreateTaskAsync(project.Id);
        await ReplaceRelatedTasksAsync(task.Id, other.Id);

        var response = await ReplaceRelatedTasksAsync(task.Id, Guid.NewGuid());

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        await AssertRelatedAsync(task.Id, other.Id);
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithoutArray_ReturnsBadRequest()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(
            await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/related-tasks", new { }),
            HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ReplaceRelatedTasks_OfUnknownTask_ReturnsNotFound()
    {
        await AssertProblemAsync(await ReplaceRelatedTasksAsync(Guid.NewGuid()), HttpStatusCode.NotFound);
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task ReplaceRelatedTasks_OnCompletedOrClosedTask_IsAllowed(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);
        var other = await CreateTaskAsync(project.Id);

        Assert.That((await ReplaceRelatedTasksAsync(task.Id, other.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id, other.Id);

        Assert.That((await ReplaceRelatedTasksAsync(task.Id)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertRelatedAsync(task.Id);
    }

    [Test]
    public async Task CreateFollowUpTask_RelatedToClosedTask_LeavesSourceTaskUnchanged()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var source = await CreateTaskInStatusAsync(project.Id, user.Id, TaskItemStatus.Closed);
        AdvanceClock();

        var followUp = await CreateTaskAsync(project.Id, user.Id, relatedTaskIds: [source.Id]);

        var sourceAfter = await GetTaskAsync(source.Id);
        Assert.Multiple(() =>
        {
            Assert.That(followUp.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(followUp.RelatedTasks.Single().Id, Is.EqualTo(source.Id));
            Assert.That(sourceAfter.RelatedTasks.Single().Id, Is.EqualTo(followUp.Id));
            Assert.That(sourceAfter.Status, Is.EqualTo(TaskItemStatus.Closed));
            Assert.That(sourceAfter.UpdatedAt, Is.EqualTo(source.UpdatedAt));
        });
    }

    private async Task AssertRelatedAsync(Guid taskId, params Guid[] expectedRelatedIds)
    {
        var task = await GetTaskAsync(taskId);

        Assert.That(task.RelatedTasks.Select(x => x.Id), Is.EquivalentTo(expectedRelatedIds));
    }
}
