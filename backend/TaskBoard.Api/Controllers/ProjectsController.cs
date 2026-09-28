using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Services.Projects;

namespace TaskBoard.Api.Controllers;

[Route("api/projects")]
public sealed class ProjectsController(IProjectService projects) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(CancellationToken cancellationToken) =>
        Ok(await projects.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);

        return project is null ? ProjectNotFound(id) : Ok(project);
    }

    /// <summary>Creates a project. Project names must be unique.</summary>
    [HttpPost]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projects.CreateAsync(request, cancellationToken);

        return result.Outcome switch
        {
            CreateProjectOutcome.Created => CreatedAtAction(
                nameof(GetById),
                new { id = result.CreatedProject!.Id },
                result.CreatedProject),
            CreateProjectOutcome.DuplicateName => DuplicateName(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await projects.UpdateAsync(id, request, cancellationToken);

        return outcome switch
        {
            UpdateProjectOutcome.Updated => NoContent(),
            UpdateProjectOutcome.NotFound => ProjectNotFound(id),
            UpdateProjectOutcome.DuplicateName => DuplicateName(),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Deletes a project. A project that still has tasks cannot be deleted (409).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await projects.DeleteAsync(id, cancellationToken);

        return outcome switch
        {
            DeleteProjectOutcome.Deleted => NoContent(),
            DeleteProjectOutcome.NotFound => ProjectNotFound(id),
            DeleteProjectOutcome.HasTasks => ConflictProblem(
                "Project has tasks",
                "A project that has tasks cannot be deleted."),
            _ => throw new UnreachableException(),
        };
    }

    private ObjectResult ProjectNotFound(Guid id) =>
        NotFoundProblem("Project not found", $"No project with ID '{id}' exists.");

    private ObjectResult DuplicateName() =>
        ConflictProblem("Duplicate project name", "Another project already uses this name.");
}
