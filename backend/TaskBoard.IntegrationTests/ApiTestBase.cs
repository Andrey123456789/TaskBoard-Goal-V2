using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Time.Testing;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Shared helpers for HTTP-level tests. Requests are sent as plain JSON objects, the way the
/// Angular client sends them; responses are read into the published response contracts.
/// </summary>
public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected static readonly Guid DeletedUserId = User.DeletedUserId;

    protected HttpClient Client { get; private set; } = null!;

    protected static FakeTimeProvider Clock => TestHost.Factory.Clock;

    [SetUp]
    public void CreateClient() => Client = TestHost.Factory.CreateClient();

    [TearDown]
    public void DisposeClient() => Client.Dispose();

    protected static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>Moves the fake clock forward so a subsequent change gets a distinct timestamp.</summary>
    protected static DateTimeOffset AdvanceClock()
    {
        Clock.Advance(TimeSpan.FromMinutes(1));
        return Clock.GetUtcNow();
    }

    protected async Task<UserDto> CreateUserAsync(string? name = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = name ?? Unique("User"), email = $"{Unique("user")}@example.com" });

        return await ReadCreatedAsync<UserDto>(response);
    }

    protected async Task<ProjectDto> CreateProjectAsync(string? name = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/projects",
            new { name = name ?? Unique("Project"), description = "Test project" });

        return await ReadCreatedAsync<ProjectDto>(response);
    }

    protected async Task<TaskDetailsDto> CreateTaskAsync(
        Guid projectId,
        Guid? assigneeId = null,
        IEnumerable<Guid>? relatedTaskIds = null,
        string? title = null)
    {
        var response = await PostTaskAsync(new
        {
            title = title ?? Unique("Task"),
            description = "Task description",
            projectId,
            assigneeId,
            relatedTaskIds,
        });

        return await ReadCreatedAsync<TaskDetailsDto>(response);
    }

    /// <summary>Creates an assigned task and moves it through the workflow to <paramref name="status"/>.</summary>
    protected async Task<TaskDetailsDto> CreateTaskInStatusAsync(Guid projectId, Guid assigneeId, TaskItemStatus status)
    {
        var task = await CreateTaskAsync(projectId, assigneeId);

        foreach (var next in new[] { TaskItemStatus.InProgress, TaskItemStatus.Completed, TaskItemStatus.Closed })
        {
            if (next > status)
            {
                break;
            }

            var response = await ChangeStatusAsync(task.Id, next);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"Moving task to {next}");
        }

        return await GetTaskAsync(task.Id);
    }

    protected Task<HttpResponseMessage> PostTaskAsync(object request) =>
        Client.PostAsJsonAsync("/api/tasks", request);

    protected Task<HttpResponseMessage> UpdateTaskAsync(Guid taskId, string title, string? description, Guid? assigneeId) =>
        Client.PutAsJsonAsync($"/api/tasks/{taskId}", new { title, description, assigneeId });

    protected Task<HttpResponseMessage> ChangeStatusAsync(Guid taskId, TaskItemStatus status) =>
        Client.PatchAsJsonAsync($"/api/tasks/{taskId}/status", new { status = status.ToString() });

    protected Task<HttpResponseMessage> ReplaceRelatedTasksAsync(Guid taskId, params Guid[] relatedTaskIds) =>
        Client.PutAsJsonAsync($"/api/tasks/{taskId}/related-tasks", new { relatedTaskIds });

    protected async Task<TaskDetailsDto> GetTaskAsync(Guid taskId)
    {
        var response = await Client.GetAsync($"/api/tasks/{taskId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return (await response.Content.ReadFromJsonAsync<TaskDetailsDto>(Json))!;
    }

    protected async Task<IReadOnlyList<TaskSummaryDto>> ListTasksAsync(string query)
    {
        var response = await Client.GetAsync($"/api/tasks{query}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return (await response.Content.ReadFromJsonAsync<List<TaskSummaryDto>>(Json))!;
    }

    protected static async Task<T> ReadCreatedAsync<T>(HttpResponseMessage response)
    {
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created),
            await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Asserts the status code and that the body is a ProblemDetails document.</summary>
    protected static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(expected), body);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var problem = JsonDocument.Parse(body).RootElement;
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo((int)expected));

        return problem;
    }
}
