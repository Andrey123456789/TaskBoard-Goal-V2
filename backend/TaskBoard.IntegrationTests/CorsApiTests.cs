using System.Net;
using TaskBoard.IntegrationTests.Support;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class CorsApiTests : ApiTestBase
{
    [Test]
    public async Task Preflight_FromConfiguredFrontendOrigin_IsAllowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks/00000000-0000-0000-0000-000000000001/status");
        request.Headers.Add("Origin", TaskBoardApiFactory.FrontendOrigin);
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await Client.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(
                response.Headers.GetValues("Access-Control-Allow-Origin"),
                Is.EqualTo(new[] { TaskBoardApiFactory.FrontendOrigin }));
            Assert.That(
                string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")),
                Does.Contain("PATCH"));
        });
    }

    [Test]
    public async Task ErrorResponse_ToFrontendOrigin_CarriesCorsHeaderSoTheClientCanReadIt()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{Guid.NewGuid()}");
        request.Headers.Add("Origin", TaskBoardApiFactory.FrontendOrigin);

        var response = await Client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
    }

    [Test]
    public async Task Request_FromUnknownOrigin_GetsNoCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await Client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }
}
