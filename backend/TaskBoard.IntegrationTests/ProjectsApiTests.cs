using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.DTOs.Projects;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class ProjectsApiTests : ApiTestBase
{
    [Test]
    public async Task CreateProject_WithValidRequest_ReturnsCreatedProject()
    {
        var name = Unique("Website");

        var response = await Client.PostAsJsonAsync("/api/projects", new { name, description = (string?)null });

        var created = await ReadCreatedAsync<ProjectDto>(response);
        Assert.Multiple(() =>
        {
            Assert.That(created.Name, Is.EqualTo(name));
            Assert.That(created.Description, Is.Null);
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/projects/{created.Id}"));
        });

        var projects = await Client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);
        Assert.That(projects!.Select(x => x.Id), Does.Contain(created.Id));
    }

    [Test]
    public async Task UpdateProject_WithValidRequest_PersistsChanges()
    {
        var project = await CreateProjectAsync();
        var newName = Unique("Renamed");

        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{project.Id}",
            new { name = newName, description = "New description" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await Client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", Json);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.Name, Is.EqualTo(newName));
            Assert.That(fetched.Description, Is.EqualTo("New description"));
        });
    }

    [Test]
    public async Task CreateProject_WithDuplicateName_ReturnsConflict()
    {
        var project = await CreateProjectAsync();

        var response = await Client.PostAsJsonAsync("/api/projects", new { name = project.Name });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task UpdateProject_ToNameOfAnotherProject_ReturnsConflict()
    {
        var first = await CreateProjectAsync();
        var second = await CreateProjectAsync();

        var response = await Client.PutAsJsonAsync($"/api/projects/{second.Id}", new { name = first.Name });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateProject_WithNameDifferingOnlyInCase_IsAllowedBecauseNamesCompareExactly()
    {
        var project = await CreateProjectAsync(Unique("CaseProject"));

        var response = await Client.PostAsJsonAsync("/api/projects", new { name = project.Name.ToUpperInvariant() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CreateProject_WithTooLongDescription_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/projects",
            new { name = Unique("P"), description = new string('d', 4001) });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteProject_WithoutTasks_RemovesProject()
    {
        var project = await CreateProjectAsync();

        var response = await Client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertProblemAsync(await Client.GetAsync($"/api/projects/{project.Id}"), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProject_WithTasks_ReturnsConflictAndKeepsProjectAndTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.DeleteAsync($"/api/projects/{project.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.That((await Client.GetAsync($"/api/projects/{project.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await GetTaskAsync(task.Id)).Project.Id, Is.EqualTo(project.Id));
    }

    [Test]
    public async Task ProjectOperations_WithUnknownId_ReturnNotFound()
    {
        var id = Guid.NewGuid();

        await AssertProblemAsync(await Client.GetAsync($"/api/projects/{id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await Client.PutAsJsonAsync($"/api/projects/{id}", new { name = Unique("P") }),
            HttpStatusCode.NotFound);
        await AssertProblemAsync(await Client.DeleteAsync($"/api/projects/{id}"), HttpStatusCode.NotFound);
    }
}
