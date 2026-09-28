using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Services.Projects;

internal sealed class ProjectService(
    IProjectRepository projects,
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken)
    {
        var allProjects = await projects.ListAsync(cancellationToken);

        return [.. allProjects.Select(ToDto)];
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);

        return project is null ? null : ToDto(project);
    }

    public async Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (await projects.NameExistsAsync(request.Name, excludedProjectId: null, cancellationToken))
        {
            return new CreateProjectResult(CreateProjectOutcome.DuplicateName, null);
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = clock.GetUtcNow(),
        };

        projects.Add(project);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return new CreateProjectResult(CreateProjectOutcome.DuplicateName, null);
        }

        return new CreateProjectResult(CreateProjectOutcome.Created, ToDto(project));
    }

    public async Task<UpdateProjectOutcome> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);

        if (project is null)
        {
            return UpdateProjectOutcome.NotFound;
        }

        if (await projects.NameExistsAsync(request.Name, excludedProjectId: id, cancellationToken))
        {
            return UpdateProjectOutcome.DuplicateName;
        }

        project.Name = request.Name;
        project.Description = request.Description;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return UpdateProjectOutcome.DuplicateName;
        }

        return UpdateProjectOutcome.Updated;
    }

    public async Task<DeleteProjectOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);

        if (project is null)
        {
            return DeleteProjectOutcome.NotFound;
        }

        if (await tasks.AnyInProjectAsync(id, cancellationToken))
        {
            return DeleteProjectOutcome.HasTasks;
        }

        projects.Remove(project);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeleteProjectOutcome.Deleted;
    }

    private static ProjectDto ToDto(Project project) =>
        new(project.Id, project.Name, project.Description, project.CreatedAt);
}
