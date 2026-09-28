namespace TaskBoard.Application.Services.Projects;

public enum DeleteProjectOutcome
{
    Deleted,
    NotFound,
    HasTasks,
}
