using System.Net;
using System.Text.Json;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class TaskFilteringApiTests : ApiTestBase
{
    [Test]
    public async Task ListTasks_FiltersByProjectAssigneeStatusAndCombinations()
    {
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        var alice = await CreateUserAsync();
        var bob = await CreateUserAsync();

        var aliceCreated = await CreateTaskAsync(project.Id, alice.Id);
        var aliceInProgress = await CreateTaskInStatusAsync(project.Id, alice.Id, TaskItemStatus.InProgress);
        var bobInProgress = await CreateTaskInStatusAsync(project.Id, bob.Id, TaskItemStatus.InProgress);
        var aliceOtherProject = await CreateTaskInStatusAsync(otherProject.Id, alice.Id, TaskItemStatus.InProgress);

        Assert.That(
            (await ListTasksAsync($"?projectId={project.Id}")).Select(x => x.Id),
            Is.EquivalentTo(new[] { aliceCreated.Id, aliceInProgress.Id, bobInProgress.Id }));

        Assert.That(
            (await ListTasksAsync($"?assigneeId={alice.Id}")).Select(x => x.Id),
            Is.EquivalentTo(new[] { aliceCreated.Id, aliceInProgress.Id, aliceOtherProject.Id }));

        var inProgress = await ListTasksAsync("?status=InProgress");
        Assert.That(inProgress.Select(x => x.Id), Is.SupersetOf(new[] { aliceInProgress.Id, bobInProgress.Id, aliceOtherProject.Id }));
        Assert.That(inProgress.Select(x => x.Status), Has.All.EqualTo(TaskItemStatus.InProgress));

        Assert.That(
            (await ListTasksAsync($"?projectId={project.Id}&assigneeId={alice.Id}&status=InProgress")).Select(x => x.Id),
            Is.EquivalentTo(new[] { aliceInProgress.Id }));
    }

    [Test]
    public async Task ListTasks_WithNoMatches_ReturnsOkWithEmptyArray()
    {
        var response = await Client.GetAsync($"/api/tasks?projectId={Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[]"));
    }

    [TestCase("?status=Reopened")]
    [TestCase("?projectId=not-a-guid")]
    public async Task ListTasks_WithMalformedFilter_ReturnsBadRequest(string query)
    {
        await AssertProblemAsync(await Client.GetAsync($"/api/tasks{query}"), HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ListTasks_SummaryContainsDashboardDataAndDistinguishesUnassignedFromDeletedUser()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var unassigned = await CreateTaskAsync(project.Id);
        var orphaned = await CreateTaskAsync(project.Id, user.Id);
        await Client.DeleteAsync($"/api/users/{user.Id}");

        var json = await Client.GetStringAsync($"/api/tasks?projectId={project.Id}");

        var tasks = JsonDocument.Parse(json).RootElement.EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
        var unassignedJson = tasks[unassigned.Id];
        var orphanedJson = tasks[orphaned.Id];
        Assert.Multiple(() =>
        {
            Assert.That(unassignedJson.GetProperty("title").GetString(), Is.EqualTo(unassigned.Title));
            Assert.That(unassignedJson.GetProperty("status").GetString(), Is.EqualTo("Created"));
            Assert.That(unassignedJson.GetProperty("project").GetProperty("id").GetGuid(), Is.EqualTo(project.Id));
            Assert.That(unassignedJson.GetProperty("project").GetProperty("name").GetString(), Is.EqualTo(project.Name));
            Assert.That(unassignedJson.GetProperty("assignee").ValueKind, Is.EqualTo(JsonValueKind.Null));

            var deletedAssignee = orphanedJson.GetProperty("assignee");
            Assert.That(deletedAssignee.GetProperty("id").GetGuid(), Is.EqualTo(DeletedUserId));
            Assert.That(deletedAssignee.GetProperty("name").GetString(), Is.EqualTo("Deleted User"));
            Assert.That(deletedAssignee.GetProperty("isDeletedUser").GetBoolean(), Is.True);
        });
    }
}
