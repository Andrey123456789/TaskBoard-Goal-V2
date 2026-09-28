using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

public sealed record TaskListFilter(Guid? ProjectId, Guid? AssigneeId, TaskItemStatus? Status);
