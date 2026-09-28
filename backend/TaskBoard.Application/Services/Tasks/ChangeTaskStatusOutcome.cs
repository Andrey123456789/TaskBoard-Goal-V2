namespace TaskBoard.Application.Services.Tasks;

public enum ChangeTaskStatusOutcome
{
    Changed,
    NotFound,

    /// <summary>The target status is not the immediately following workflow status.</summary>
    InvalidTransition,

    /// <summary>Starting work requires an assigned active ordinary user.</summary>
    ActiveAssigneeRequired,
}
