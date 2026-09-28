namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// Editable core task fields. Project and status cannot be changed through this request.
/// </summary>
public sealed record UpdateTaskRequest(string Title, string? Description, Guid? AssigneeId);
