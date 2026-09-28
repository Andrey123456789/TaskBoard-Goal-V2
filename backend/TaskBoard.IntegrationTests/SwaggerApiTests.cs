using System.Net;
using Serilog.Events;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class SwaggerApiTests
{
    [Test]
    public async Task DevelopmentHost_ServesSwaggerUiAndDocumentForTheRealRoutes()
    {
        // The Development host runs startup seeding; pointing it at a database that does not
        // exist makes seeding skip (pending migrations) without touching any real database.
        await using var factory = new TaskBoardApiFactory(TestDatabase.MissingDatabaseConnectionString(), "Development");
        using var client = factory.CreateClient();

        var ui = await client.GetAsync("/swagger/index.html");
        var document = await client.GetStringAsync("/swagger/taskboard/swagger.json");

        Assert.Multiple(() =>
        {
            Assert.That(ui.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ui.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(document, Does.Contain("\"/api/users\""));
            Assert.That(document, Does.Contain("\"/api/projects/{id}\""));
            Assert.That(document, Does.Contain("\"/api/tasks/{id}/status\""));
            Assert.That(document, Does.Contain("\"/api/tasks/{id}/related-tasks\""));
            Assert.That(document, Does.Not.Contain("/api/v"));
            Assert.That(
                factory.LogSink.Events.Any(x => x.Level == LogEventLevel.Warning
                    && x.MessageTemplate.Text.Contains("skipping development seeding")),
                Is.True);
        });
    }

    [Test]
    public async Task NonDevelopmentHost_DoesNotExposeSwagger()
    {
        using var client = TestHost.Factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
