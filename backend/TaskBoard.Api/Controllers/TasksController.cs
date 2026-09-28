using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Services.Tasks;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Api.Controllers;

[Route("api/tasks")]
public sealed class TasksController(ITaskService tasks) : ApiControllerBase
{
    /// <summary>
    /// Lists task summaries. Optional filters can be combined; no matches returns an empty array.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryDto>>> List(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] TaskItemStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await tasks.ListAsync(new TaskListFilter(projectId, assigneeId, status), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);

        return task is null ? TaskNotFound(id) : Ok(task);
    }

    /// <summary>
    /// Creates a task in status Created. Referencing a missing project, assignee, or related
    /// task, or assigning "Deleted User", returns 422.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TaskDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDetailsDto>> Create(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tasks.CreateAsync(request, cancellationToken);

        return result.Outcome switch
        {
            CreateTaskOutcome.Created => CreatedAtAction(
                nameof(GetById),
                new { id = result.CreatedTask!.Id },
                result.CreatedTask),
            CreateTaskOutcome.ProjectNotFound => UnprocessableProblem(
                "Project not found",
                "The referenced project does not exist."),
            CreateTaskOutcome.AssigneeNotFound => AssigneeNotFound(),
            CreateTaskOutcome.DeletedUserNotAssignable => DeletedUserNotAssignable(),
            CreateTaskOutcome.RelatedTaskNotFound => RelatedTaskNotFound(),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// Updates title, description, and assignee. Completed and closed tasks are read-only (409).
    /// An InProgress task cannot be unassigned (422).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var outcome = await tasks.UpdateAsync(id, request, cancellationToken);

        return outcome switch
        {
            UpdateTaskOutcome.Updated => NoContent(),
            UpdateTaskOutcome.NotFound => TaskNotFound(id),
            UpdateTaskOutcome.NotEditable => ConflictProblem(
                "Task is read-only",
                "Completed and closed tasks cannot be edited."),
            UpdateTaskOutcome.AssigneeNotFound => AssigneeNotFound(),
            UpdateTaskOutcome.DeletedUserNotAssignable => DeletedUserNotAssignable(),
            UpdateTaskOutcome.AssigneeRequired => UnprocessableProblem(
                "Assignee required",
                "An InProgress task cannot be unassigned."),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// Moves the task to the immediately following status
    /// (Created → InProgress → Completed → Closed). Any other transition returns 409.
    /// Starting a task requires an assigned active user (409 otherwise).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await tasks.ChangeStatusAsync(id, request.Status!.Value, cancellationToken);

        return outcome switch
        {
            ChangeTaskStatusOutcome.Changed => NoContent(),
            ChangeTaskStatusOutcome.NotFound => TaskNotFound(id),
            ChangeTaskStatusOutcome.InvalidTransition => ConflictProblem(
                "Invalid status transition",
                $"A task can only move to the next status (Created → InProgress → Completed → Closed); "
                + $"'{request.Status}' is not the next status of this task."),
            ChangeTaskStatusOutcome.ActiveAssigneeRequired => ConflictProblem(
                "Active assignee required",
                "A task can be started only when it is assigned to an active user."),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// Replaces the complete set of related tasks. Relationships are symmetric; duplicate IDs are
    /// ignored; an empty array removes all relationships. Allowed in every status and does not
    /// change UpdatedAt.
    /// </summary>
    [HttpPut("{id:guid}/related-tasks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReplaceRelatedTasks(
        Guid id,
        ReplaceRelatedTasksRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await tasks.ReplaceRelatedTasksAsync(id, request.RelatedTaskIds!, cancellationToken);

        return outcome switch
        {
            ReplaceRelatedTasksOutcome.Replaced => NoContent(),
            ReplaceRelatedTasksOutcome.NotFound => TaskNotFound(id),
            ReplaceRelatedTasksOutcome.SelfReference => UnprocessableProblem(
                "Self reference",
                "A task cannot be related to itself."),
            ReplaceRelatedTasksOutcome.RelatedTaskNotFound => RelatedTaskNotFound(),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Deletes a Created or InProgress task and all of its relationships.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await tasks.DeleteAsync(id, cancellationToken);

        return outcome switch
        {
            DeleteTaskOutcome.Deleted => NoContent(),
            DeleteTaskOutcome.NotFound => TaskNotFound(id),
            DeleteTaskOutcome.NotDeletable => ConflictProblem(
                "Task cannot be deleted",
                "Completed and closed tasks cannot be deleted."),
            _ => throw new UnreachableException(),
        };
    }

    private ObjectResult TaskNotFound(Guid id) =>
        NotFoundProblem("Task not found", $"No task with ID '{id}' exists.");

    private ObjectResult AssigneeNotFound() =>
        UnprocessableProblem("Assignee not found", "The referenced assignee does not exist.");

    private ObjectResult DeletedUserNotAssignable() =>
        UnprocessableProblem("Deleted User cannot be assigned", "Tasks cannot be manually assigned to Deleted User.");

    private ObjectResult RelatedTaskNotFound() =>
        UnprocessableProblem("Related task not found", "One or more related task IDs do not reference existing tasks.");
}
