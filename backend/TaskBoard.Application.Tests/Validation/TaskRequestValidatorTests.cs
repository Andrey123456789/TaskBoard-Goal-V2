using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Tests.Validation;

[TestFixture]
public sealed class TaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _createValidator = new();
    private readonly UpdateTaskRequestValidator _updateValidator = new();
    private readonly ChangeTaskStatusRequestValidator _statusValidator = new();
    private readonly ReplaceRelatedTasksRequestValidator _relatedValidator = new();

    [Test]
    public void Create_WithMinimalValidRequest_Passes()
    {
        var result = _createValidator.Validate(new CreateTaskRequest("Title", null, Guid.NewGuid(), null, null));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Create_WithEmptyTitleAndProject_FailsOnBoth()
    {
        var result = _createValidator.Validate(new CreateTaskRequest(" ", null, Guid.Empty, null, null));

        Assert.That(
            result.Errors.Select(x => x.PropertyName),
            Is.EquivalentTo(new[] { "Title", "ProjectId" }));
    }

    [Test]
    public void Create_WithTitleOverMaximumLength_Fails()
    {
        var result = _createValidator.Validate(
            new CreateTaskRequest(new string('t', 201), null, Guid.NewGuid(), null, null));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Title" }));
    }

    [Test]
    public void Update_WithDescriptionOverMaximumLength_Fails()
    {
        var result = _updateValidator.Validate(new UpdateTaskRequest("Title", new string('d', 4001), null));

        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "Description" }));
    }

    [Test]
    public void Update_WithTitleAndDescriptionAtMaximumLength_Passes()
    {
        var result = _updateValidator.Validate(
            new UpdateTaskRequest(new string('t', 200), new string('d', 4000), Guid.NewGuid()));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ChangeStatus_WithoutStatus_Fails()
    {
        var result = _statusValidator.Validate(new ChangeTaskStatusRequest(null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ChangeStatus_WithUndefinedStatusValue_Fails()
    {
        var result = _statusValidator.Validate(new ChangeTaskStatusRequest((TaskItemStatus)42));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ReplaceRelatedTasks_WithoutArray_Fails()
    {
        var result = _relatedValidator.Validate(new ReplaceRelatedTasksRequest(null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ReplaceRelatedTasks_WithEmptyArray_Passes()
    {
        var result = _relatedValidator.Validate(new ReplaceRelatedTasksRequest([]));

        Assert.That(result.IsValid, Is.True);
    }
}
