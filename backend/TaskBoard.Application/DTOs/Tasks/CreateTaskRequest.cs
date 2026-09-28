namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// A new task always starts in <c>Created</c>; the initial status cannot be supplied.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Guid ProjectId,
    Guid? AssigneeId,
    IReadOnlyList<Guid>? RelatedTaskIds);
