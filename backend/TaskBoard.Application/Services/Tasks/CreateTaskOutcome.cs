namespace TaskBoard.Application.Services.Tasks;

public enum CreateTaskOutcome
{
    Created,
    ProjectNotFound,
    AssigneeNotFound,
    DeletedUserNotAssignable,
    RelatedTaskNotFound,
}
