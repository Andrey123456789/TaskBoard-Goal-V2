using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

public class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? AssigneeId { get; set; }

    public User? Assignee { get; set; }

    public TaskItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent persisted change to the task's core state
    /// (title, description, assignee, status). Related-task changes do not affect it.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
