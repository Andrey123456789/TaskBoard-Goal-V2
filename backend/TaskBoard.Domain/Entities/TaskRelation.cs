namespace TaskBoard.Domain.Entities;

/// <summary>
/// A symmetric relationship between two tasks. Each relationship is stored once;
/// <c>(A, B)</c> and <c>(B, A)</c> represent the same relationship.
/// </summary>
public class TaskRelation
{
    public Guid FirstTaskId { get; set; }

    public TaskItem? FirstTask { get; set; }

    public Guid SecondTaskId { get; set; }

    public TaskItem? SecondTask { get; set; }
}
