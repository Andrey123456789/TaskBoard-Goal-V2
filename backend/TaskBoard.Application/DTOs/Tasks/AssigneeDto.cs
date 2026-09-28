namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// The current assignee of a task. <see cref="IsDeletedUser"/> is <c>true</c> when the
/// task was reassigned to the system <c>Deleted User</c>; an unassigned task has no assignee.
/// </summary>
public sealed record AssigneeDto(Guid Id, string Name, bool IsDeletedUser);
