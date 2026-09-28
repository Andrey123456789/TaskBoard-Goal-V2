using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Validation;

namespace TaskBoard.Application.Tests.Validation;

[TestFixture]
public sealed class ProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _createValidator = new();
    private readonly UpdateProjectRequestValidator _updateValidator = new();

    [Test]
    public void Validate_WithNameAndNoDescription_Passes()
    {
        var result = _createValidator.Validate(new CreateProjectRequest("Website", null));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Validate_WithEmptyName_Fails(string name)
    {
        var result = _updateValidator.Validate(new UpdateProjectRequest(name, null));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Name" }));
    }

    [Test]
    public void Validate_WithNameOverMaximumLength_Fails()
    {
        var result = _createValidator.Validate(new CreateProjectRequest(new string('p', 201), null));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Name" }));
    }

    [Test]
    public void Validate_WithDescriptionAtMaximumLength_Passes()
    {
        var result = _updateValidator.Validate(new UpdateProjectRequest("Website", new string('d', 4000)));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithDescriptionOverMaximumLength_Fails()
    {
        var result = _createValidator.Validate(new CreateProjectRequest("Website", new string('d', 4001)));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Description" }));
    }
}
