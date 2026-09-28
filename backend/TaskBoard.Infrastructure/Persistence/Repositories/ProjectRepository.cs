using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludedProjectId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(x => x.Name == name && x.Id != excludedProjectId, cancellationToken);

    public void Add(Project project) => db.Projects.Add(project);

    public void Remove(Project project) => db.Projects.Remove(project);
}
