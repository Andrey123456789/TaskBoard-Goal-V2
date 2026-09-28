namespace TaskBoard.Domain.Entities;

public class Project
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
