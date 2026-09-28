using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Services.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskSummaryDto>> ListAsync(TaskListFilter filter, CancellationToken cancellationToken);

    Task<TaskDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateTaskResult> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<UpdateTaskOutcome> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<ChangeTaskStatusOutcome> ChangeStatusAsync(
        Guid id,
        TaskItemStatus targetStatus,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the complete related-task set. Does not change the task's <c>UpdatedAt</c>.
    /// </summary>
    Task<ReplaceRelatedTasksOutcome> ReplaceRelatedTasksAsync(
        Guid id,
        IReadOnlyCollection<Guid> relatedTaskIds,
        CancellationToken cancellationToken);

    Task<DeleteTaskOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
