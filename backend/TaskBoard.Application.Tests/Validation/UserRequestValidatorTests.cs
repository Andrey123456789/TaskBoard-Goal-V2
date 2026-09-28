using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Validation;

namespace TaskBoard.Application.Tests.Validation;

[TestFixture]
public sealed class UserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _createValidator = new();
    private readonly UpdateUserRequestValidator _updateValidator = new();

    [Test]
    public void Validate_WithValidNameAndEmail_Passes()
    {
        var result = _createValidator.Validate(new CreateUserRequest("Alice", "alice@example.com"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WithEmptyName_FailsOnName(string name)
    {
        var result = _createValidator.Validate(new CreateUserRequest(name, "alice@example.com"));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Name" }));
    }

    [TestCase("")]
    [TestCase("not-an-email")]
    [TestCase("@example.com")]
    public void Validate_WithMissingOrInvalidEmail_FailsOnceOnEmail(string email)
    {
        var result = _updateValidator.Validate(new UpdateUserRequest("Alice", email));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Email" }));
    }

    [Test]
    public void Validate_WithNameAtMaximumLength_Passes()
    {
        var result = _createValidator.Validate(new CreateUserRequest(new string('a', 200), "alice@example.com"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithNameOverMaximumLength_Fails()
    {
        var result = _updateValidator.Validate(new UpdateUserRequest(new string('a', 201), "alice@example.com"));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Name" }));
    }

    [Test]
    public void Validate_WithEmailOverMaximumLength_Fails()
    {
        var email = new string('a', 309) + "@example.com"; // 321 characters

        var result = _createValidator.Validate(new CreateUserRequest("Alice", email));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Email" }));
    }
}
