namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// The complete desired set of related task IDs. An empty array removes all relationships.
/// </summary>
public sealed record ReplaceRelatedTasksRequest(IReadOnlyList<Guid>? RelatedTaskIds);
