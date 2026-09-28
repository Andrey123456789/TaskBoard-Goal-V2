namespace TaskBoard.Application.Services.Tasks;

public enum UpdateTaskOutcome
{
    Updated,
    NotFound,

    /// <summary>The task is <c>Completed</c> or <c>Closed</c>; its core data is read-only.</summary>
    NotEditable,

    AssigneeNotFound,
    DeletedUserNotAssignable,

    /// <summary>An <c>InProgress</c> task cannot be manually unassigned.</summary>
    AssigneeRequired,
}
