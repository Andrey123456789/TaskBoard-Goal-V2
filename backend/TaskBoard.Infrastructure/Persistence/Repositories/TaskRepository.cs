using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    private static readonly Expression<Func<TaskItem, TaskSummaryDto>> ToSummary = x => new TaskSummaryDto(
        x.Id,
        x.Title,
        x.Status,
        new ProjectSummaryDto(x.ProjectId, x.Project!.Name),
        x.Assignee == null
            ? null
            : new AssigneeDto(x.Assignee.Id, x.Assignee.Name, x.Assignee.Id == User.DeletedUserId));

    public async Task<IReadOnlyList<TaskSummaryDto>> ListSummariesAsync(
        TaskListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Tasks.AsQueryable();

        if (filter.ProjectId is { } projectId)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (filter.AssigneeId is { } assigneeId)
        {
            query = query.Where(x => x.AssigneeId == assigneeId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                Project = new ProjectSummaryDto(x.ProjectId, x.Project!.Name),
                Assignee = x.Assignee == null
                    ? null
                    : new AssigneeDto(x.Assignee.Id, x.Assignee.Name, x.Assignee.Id == User.DeletedUserId),
                x.CreatedAt,
                x.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return null;
        }

        var relatedTaskIds = RelatedTaskIds(id);

        var relatedTasks = await db.Tasks
            .Where(x => relatedTaskIds.Contains(x.Id))
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

        return new TaskDetailsDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Project,
            task.Assignee,
            task.CreatedAt,
            task.UpdatedAt,
            relatedTasks);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Tasks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Tasks.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await db.Tasks
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyInProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.Tasks.AnyAsync(x => x.ProjectId == projectId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await db.Tasks
            .Where(x => x.AssigneeId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetRelatedTaskIdsAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        await RelatedTaskIds(taskId).ToListAsync(cancellationToken);

    public void Add(TaskItem task) => db.Tasks.Add(task);

    public async Task RemoveAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var relations = await db.TaskRelations
            .Where(x => x.FirstTaskId == task.Id || x.SecondTaskId == task.Id)
            .ToListAsync(cancellationToken);

        db.TaskRelations.RemoveRange(relations);
        db.Tasks.Remove(task);
    }

    public void AddRelations(Guid taskId, IEnumerable<Guid> relatedTaskIds)
    {
        foreach (var relatedTaskId in relatedTaskIds)
        {
            db.TaskRelations.Add(TaskRelationKey.Create(taskId, relatedTaskId));
        }
    }

    public async Task RemoveRelationsAsync(
        Guid taskId,
        IReadOnlyCollection<Guid> relatedTaskIds,
        CancellationToken cancellationToken)
    {
        if (relatedTaskIds.Count == 0)
        {
            return;
        }

        var relations = await db.TaskRelations
            .Where(x =>
                (x.FirstTaskId == taskId && relatedTaskIds.Contains(x.SecondTaskId))
                || (x.SecondTaskId == taskId && relatedTaskIds.Contains(x.FirstTaskId)))
            .ToListAsync(cancellationToken);

        db.TaskRelations.RemoveRange(relations);
    }

    private IQueryable<Guid> RelatedTaskIds(Guid taskId) =>
        db.TaskRelations
            .Where(x => x.FirstTaskId == taskId)
            .Select(x => x.SecondTaskId)
            .Concat(db.TaskRelations
                .Where(x => x.SecondTaskId == taskId)
                .Select(x => x.FirstTaskId));
}
