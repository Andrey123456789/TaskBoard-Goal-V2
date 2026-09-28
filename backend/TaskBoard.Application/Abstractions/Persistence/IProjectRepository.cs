using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken);

    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludedProjectId, CancellationToken cancellationToken);

    void Add(Project project);

    void Remove(Project project);
}
