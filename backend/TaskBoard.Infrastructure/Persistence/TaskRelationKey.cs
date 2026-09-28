using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence;

/// <summary>
/// Stores each symmetric relationship once by always putting the smaller task ID first,
/// so <c>(A, B)</c> and <c>(B, A)</c> map to the same primary key.
/// </summary>
internal static class TaskRelationKey
{
    public static TaskRelation Create(Guid taskId, Guid relatedTaskId) =>
        taskId.CompareTo(relatedTaskId) < 0
            ? new TaskRelation { FirstTaskId = taskId, SecondTaskId = relatedTaskId }
            : new TaskRelation { FirstTaskId = relatedTaskId, SecondTaskId = taskId };
}
