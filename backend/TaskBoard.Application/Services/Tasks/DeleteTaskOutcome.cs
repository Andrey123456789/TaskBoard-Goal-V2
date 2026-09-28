namespace TaskBoard.Application.Services.Tasks;

public enum DeleteTaskOutcome
{
    Deleted,
    NotFound,

    /// <summary>The task is <c>Completed</c> or <c>Closed</c> and cannot be deleted.</summary>
    NotDeletable,
}
