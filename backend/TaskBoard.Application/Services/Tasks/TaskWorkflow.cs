using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Services.Tasks;

/// <summary>
/// Task status workflow rules: <c>Created -&gt; InProgress -&gt; Completed -&gt; Closed</c>.
/// Only the immediately following transition is allowed; status never moves backward.
/// </summary>
internal static class TaskWorkflow
{
    public static TaskItemStatus? GetNextStatus(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Created => TaskItemStatus.InProgress,
        TaskItemStatus.InProgress => TaskItemStatus.Completed,
        TaskItemStatus.Completed => TaskItemStatus.Closed,
        _ => null,
    };

    public static bool IsTransitionAllowed(TaskItemStatus current, TaskItemStatus target) =>
        GetNextStatus(current) == target;

    /// <summary>
    /// Title, description, and assignee are editable only before completion.
    /// </summary>
    public static bool IsCoreEditable(TaskItemStatus status) =>
        status is TaskItemStatus.Created or TaskItemStatus.InProgress;

    public static bool IsDeletable(TaskItemStatus status) =>
        status is TaskItemStatus.Created or TaskItemStatus.InProgress;
}
