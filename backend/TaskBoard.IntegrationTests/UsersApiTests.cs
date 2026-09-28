using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.IntegrationTests;

[TestFixture]
public sealed class UsersApiTests : ApiTestBase
{
    [Test]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUserWithLocation()
    {
        var email = $"{Unique("alice")}@example.com";

        var response = await Client.PostAsJsonAsync("/api/users", new { name = "Alice", email });

        var created = await ReadCreatedAsync<UserDto>(response);
        Assert.Multiple(() =>
        {
            Assert.That(created.Name, Is.EqualTo("Alice"));
            Assert.That(created.Email, Is.EqualTo(email));
            Assert.That(created.CreatedAt, Is.EqualTo(Clock.GetUtcNow()));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/users/{created.Id}"));
        });

        var fetched = await Client.GetFromJsonAsync<UserDto>($"/api/users/{created.Id}", Json);
        Assert.That(fetched, Is.EqualTo(created));
    }

    [Test]
    public async Task CreateUser_WithEmailDifferingOnlyInCase_ReturnsConflict()
    {
        var email = $"{Unique("bob")}@example.com";
        await ReadCreatedAsync<UserDto>(await Client.PostAsJsonAsync("/api/users", new { name = "Bob", email }));

        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = "Other Bob", email = email.ToUpperInvariant() });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [TestCase("", "valid@example.com")]
    [TestCase("Name", "")]
    [TestCase("Name", "not-an-email")]
    public async Task CreateUser_WithInvalidInput_ReturnsValidationProblem(string name, string email)
    {
        var response = await Client.PostAsJsonAsync("/api/users", new { name, email });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(problem.TryGetProperty("errors", out _), Is.True);
    }

    [Test]
    public async Task CreateUser_WithNameOverMaximumLength_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = new string('n', 201), email = $"{Unique("long")}@example.com" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateUser_WithValidRequest_PersistsChanges()
    {
        var user = await CreateUserAsync();
        var newEmail = $"{Unique("renamed")}@example.com";

        var response = await Client.PutAsJsonAsync($"/api/users/{user.Id}", new { name = "Renamed", email = newEmail });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var fetched = await Client.GetFromJsonAsync<UserDto>($"/api/users/{user.Id}", Json);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.Name, Is.EqualTo("Renamed"));
            Assert.That(fetched.Email, Is.EqualTo(newEmail));
        });
    }

    [Test]
    public async Task UpdateUser_ChangingOnlyTheCaseOfOwnEmail_Succeeds()
    {
        var user = await CreateUserAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new { name = user.Name, email = user.Email.ToUpperInvariant() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task UpdateUser_ToEmailOfAnotherUser_ReturnsConflict()
    {
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{second.Id}",
            new { name = second.Name, email = first.Email });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task DeleteUser_WithoutTasks_RemovesUser()
    {
        var user = await CreateUserAsync();

        var response = await Client.DeleteAsync($"/api/users/{user.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await AssertProblemAsync(await Client.GetAsync($"/api/users/{user.Id}"), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ListUsers_ReturnsOrdinaryUsersButNeverDeletedUser()
    {
        var user = await CreateUserAsync();

        var users = await Client.GetFromJsonAsync<List<UserDto>>("/api/users", Json);

        Assert.Multiple(() =>
        {
            Assert.That(users!.Select(x => x.Id), Does.Contain(user.Id));
            Assert.That(users!.Select(x => x.Id), Does.Not.Contain(DeletedUserId));
        });
    }

    [Test]
    public async Task DeletedUser_IsNotAddressableThroughUserOperations()
    {
        await AssertProblemAsync(await Client.GetAsync($"/api/users/{DeletedUserId}"), HttpStatusCode.NotFound);

        await AssertProblemAsync(
            await Client.PutAsJsonAsync(
                $"/api/users/{DeletedUserId}",
                new { name = "Renamed", email = $"{Unique("x")}@example.com" }),
            HttpStatusCode.NotFound);

        await AssertProblemAsync(await Client.DeleteAsync($"/api/users/{DeletedUserId}"), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UserOperations_WithUnknownId_ReturnNotFound()
    {
        var id = Guid.NewGuid();

        await AssertProblemAsync(await Client.GetAsync($"/api/users/{id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(await Client.DeleteAsync($"/api/users/{id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await Client.PutAsJsonAsync($"/api/users/{id}", new { name = "N", email = "n@example.com" }),
            HttpStatusCode.NotFound);
    }
}
