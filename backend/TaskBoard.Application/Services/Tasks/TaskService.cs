using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Services.Tasks;

internal sealed class TaskService(
    ITaskRepository tasks,
    IProjectRepository projects,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ITaskService
{
    public Task<IReadOnlyList<TaskSummaryDto>> ListAsync(TaskListFilter filter, CancellationToken cancellationToken) =>
        tasks.ListSummariesAsync(filter, cancellationToken);

    public Task<TaskDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        tasks.GetDetailsAsync(id, cancellationToken);

    public async Task<CreateTaskResult> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await projects.ExistsAsync(request.ProjectId, cancellationToken))
        {
            return new CreateTaskResult(CreateTaskOutcome.ProjectNotFound, null);
        }

        if (request.AssigneeId is { } assigneeId)
        {
            switch (await CheckAssignableAsync(assigneeId, cancellationToken))
            {
                case Assignability.DeletedUser:
                    return new CreateTaskResult(CreateTaskOutcome.DeletedUserNotAssignable, null);
                case Assignability.NotFound:
                    return new CreateTaskResult(CreateTaskOutcome.AssigneeNotFound, null);
            }
        }

        var relatedTaskIds = (request.RelatedTaskIds ?? []).ToHashSet();

        if (!await AllTasksExistAsync(relatedTaskIds, cancellationToken))
        {
            return new CreateTaskResult(CreateTaskOutcome.RelatedTaskNotFound, null);
        }

        var now = clock.GetUtcNow();
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            AssigneeId = request.AssigneeId,
            Status = TaskItemStatus.Created,
            CreatedAt = now,
            UpdatedAt = now,
        };

        tasks.Add(task);
        tasks.AddRelations(task.Id, relatedTaskIds);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var createdTask = await tasks.GetDetailsAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException("The created task could not be reloaded.");

        return new CreateTaskResult(CreateTaskOutcome.Created, createdTask);
    }

    public async Task<UpdateTaskOutcome> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);

        if (task is null)
        {
            return UpdateTaskOutcome.NotFound;
        }

        if (!TaskWorkflow.IsCoreEditable(task.Status))
        {
            return UpdateTaskOutcome.NotEditable;
        }

        // Only an actual assignee change is validated: keeping the current assignee unchanged
        // (including a system-assigned Deleted User) is not a manual assignment.
        if (request.AssigneeId != task.AssigneeId)
        {
            if (request.AssigneeId is { } assigneeId)
            {
                switch (await CheckAssignableAsync(assigneeId, cancellationToken))
                {
                    case Assignability.DeletedUser:
                        return UpdateTaskOutcome.DeletedUserNotAssignable;
                    case Assignability.NotFound:
                        return UpdateTaskOutcome.AssigneeNotFound;
                }
            }
            else if (task.Status == TaskItemStatus.InProgress)
            {
                return UpdateTaskOutcome.AssigneeRequired;
            }
        }

        var hasChanges =
            task.Title != request.Title
            || task.Description != request.Description
            || task.AssigneeId != request.AssigneeId;

        if (!hasChanges)
        {
            return UpdateTaskOutcome.Updated;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = clock.GetUtcNow();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateTaskOutcome.Updated;
    }

    public async Task<ChangeTaskStatusOutcome> ChangeStatusAsync(
        Guid id,
        TaskItemStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);

        if (task is null)
        {
            return ChangeTaskStatusOutcome.NotFound;
        }

        if (!TaskWorkflow.IsTransitionAllowed(task.Status, targetStatus))
        {
            return ChangeTaskStatusOutcome.InvalidTransition;
        }

        if (targetStatus == TaskItemStatus.InProgress && !HasActiveOrdinaryAssignee(task))
        {
            return ChangeTaskStatusOutcome.ActiveAssigneeRequired;
        }

        task.Status = targetStatus;
        task.UpdatedAt = clock.GetUtcNow();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ChangeTaskStatusOutcome.Changed;
    }

    public async Task<ReplaceRelatedTasksOutcome> ReplaceRelatedTasksAsync(
        Guid id,
        IReadOnlyCollection<Guid> relatedTaskIds,
        CancellationToken cancellationToken)
    {
        if (!await tasks.ExistsAsync(id, cancellationToken))
        {
            return ReplaceRelatedTasksOutcome.NotFound;
        }

        var desired = relatedTaskIds.ToHashSet();

        if (desired.Contains(id))
        {
            return ReplaceRelatedTasksOutcome.SelfReference;
        }

        if (!await AllTasksExistAsync(desired, cancellationToken))
        {
            return ReplaceRelatedTasksOutcome.RelatedTaskNotFound;
        }

        var current = await tasks.GetRelatedTaskIdsAsync(id, cancellationToken);
        var toAdd = desired.Except(current).ToList();
        var toRemove = current.Except(desired).ToList();

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return ReplaceRelatedTasksOutcome.Replaced;
        }

        // Relationship changes deliberately leave the task's core state and UpdatedAt untouched.
        tasks.AddRelations(id, toAdd);
        await tasks.RemoveRelationsAsync(id, toRemove, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ReplaceRelatedTasksOutcome.Replaced;
    }

    public async Task<DeleteTaskOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);

        if (task is null)
        {
            return DeleteTaskOutcome.NotFound;
        }

        if (!TaskWorkflow.IsDeletable(task.Status))
        {
            return DeleteTaskOutcome.NotDeletable;
        }

        await tasks.RemoveAsync(task, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeleteTaskOutcome.Deleted;
    }

    private static bool HasActiveOrdinaryAssignee(TaskItem task) =>
        task.AssigneeId is { } assigneeId && assigneeId != User.DeletedUserId;

    private async Task<Assignability> CheckAssignableAsync(Guid assigneeId, CancellationToken cancellationToken)
    {
        if (assigneeId == User.DeletedUserId)
        {
            return Assignability.DeletedUser;
        }

        return await users.ExistsAsync(assigneeId, cancellationToken)
            ? Assignability.Assignable
            : Assignability.NotFound;
    }

    private async Task<bool> AllTasksExistAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return true;
        }

        var existingIds = await tasks.GetExistingIdsAsync(taskIds, cancellationToken);

        return existingIds.Count == taskIds.Count;
    }

    private enum Assignability
    {
        Assignable,
        NotFound,
        DeletedUser,
    }
}
