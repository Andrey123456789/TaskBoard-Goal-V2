using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Persistence boundary for tasks and their symmetric task relationships.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskSummaryDto>> ListSummariesAsync(
        TaskListFilter filter,
        CancellationToken cancellationToken);

    Task<TaskDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the subset of <paramref name="ids"/> that identify existing tasks.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> AnyInProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetRelatedTaskIdsAsync(Guid taskId, CancellationToken cancellationToken);

    void Add(TaskItem task);

    /// <summary>
    /// Stages removal of the task together with every relationship involving it.
    /// </summary>
    Task RemoveAsync(TaskItem task, CancellationToken cancellationToken);

    /// <summary>
    /// Stages new relationships between <paramref name="taskId"/> and each of
    /// <paramref name="relatedTaskIds"/>. The caller must not pass existing relationships.
    /// </summary>
    void AddRelations(Guid taskId, IEnumerable<Guid> relatedTaskIds);

    Task RemoveRelationsAsync(
        Guid taskId,
        IReadOnlyCollection<Guid> relatedTaskIds,
        CancellationToken cancellationToken);
}
