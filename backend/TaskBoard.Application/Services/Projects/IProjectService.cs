using TaskBoard.Application.DTOs.Projects;

namespace TaskBoard.Application.Services.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken);

    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateProjectResult> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task<UpdateProjectOutcome> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a project that has no tasks. Tasks are never cascade-deleted.
    /// </summary>
    Task<DeleteProjectOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
