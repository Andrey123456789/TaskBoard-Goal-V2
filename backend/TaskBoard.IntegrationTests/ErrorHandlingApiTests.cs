using System.Net;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Events;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Services.Projects;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class ErrorHandlingApiTests : ApiTestBase
{
    private const string SensitiveMessage =
        "Boom: Server=prod-sql;Password=hunter2; SELECT * FROM Users; at C:\\internal\\secret\\path.cs";

    [Test]
    public async Task UnexpectedException_ReturnsSanitizedProblemDetailsAndIsLoggedServerSide()
    {
        await using var factory = TestHost.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProjectService>();
                services.AddScoped<IProjectService, ThrowingProjectService>();
            }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        var body = problem.GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(problem.GetProperty("title").GetString(), Is.EqualTo("An unexpected error occurred."));
            Assert.That(problem.TryGetProperty("traceId", out _), Is.True);
            Assert.That(body, Does.Not.Contain("Boom"));
            Assert.That(body, Does.Not.Contain("Password"));
            Assert.That(body, Does.Not.Contain("SELECT"));
            Assert.That(body, Does.Not.Contain("internal"));
            Assert.That(body, Does.Not.Contain(nameof(InvalidOperationException)));
            Assert.That(body, Does.Not.Contain(nameof(ThrowingProjectService)));
        });

        var logged = TestHost.Factory.LogSink.Events
            .Where(x => x.Exception?.Message == SensitiveMessage)
            .ToList();
        Assert.That(logged, Has.Count.EqualTo(1), "The unexpected exception must be logged exactly once.");
        Assert.That(logged[0].Level, Is.EqualTo(LogEventLevel.Error));
    }

    [Test]
    public async Task BusinessConflict_ReturnsDocumentedProblemDetails()
    {
        var project = await CreateProjectAsync();

        var response = await Client.PostAsync(
            "/api/projects",
            new StringContent($$"""{"name":"{{project.Name}}"}""", Encoding.UTF8, "application/json"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.That(problem.GetProperty("title").GetString(), Is.EqualTo("Duplicate project name"));
    }

    [Test]
    public async Task MalformedJson_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            "/api/tasks",
            new StringContent("{ \"title\": ", Encoding.UTF8, "application/json"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UnknownRoute_ReturnsNotFoundProblemDetails()
    {
        await AssertProblemAsync(await Client.GetAsync("/api/does-not-exist"), HttpStatusCode.NotFound);
    }

    private sealed class ThrowingProjectService : IProjectService
    {
        public Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<CreateProjectResult> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<UpdateProjectOutcome> UpdateAsync(
            Guid id,
            UpdateProjectRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<DeleteProjectOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SensitiveMessage);
    }
}
