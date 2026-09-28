namespace TaskBoard.Application.Services.Tasks;

public enum ReplaceRelatedTasksOutcome
{
    Replaced,
    NotFound,
    SelfReference,
    RelatedTaskNotFound,
}
