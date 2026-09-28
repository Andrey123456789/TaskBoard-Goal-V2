namespace TaskBoard.Domain.Entities;

public class User
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 320;

    /// <summary>
    /// Well-known identifier of the system <c>Deleted User</c> record. Tasks are
    /// reassigned to this user when their assignee is deleted.
    /// </summary>
    public static readonly Guid DeletedUserId = new("5d1e7e1d-0000-4000-8000-00000000de1e");

    public const string DeletedUserName = "Deleted User";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
