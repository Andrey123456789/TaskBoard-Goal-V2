using TaskBoard.Application.DTOs.Projects;

namespace TaskBoard.Application.Services.Projects;

public sealed record CreateProjectResult(CreateProjectOutcome Outcome, ProjectDto? CreatedProject);
