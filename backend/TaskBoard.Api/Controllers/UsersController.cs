using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Services.Users;

namespace TaskBoard.Api.Controllers;

/// <summary>
/// Ordinary users. The system "Deleted User" is not exposed as a user resource.
/// </summary>
[Route("api/users")]
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Lists ordinary assignable users (never includes "Deleted User").</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);

        return user is null ? UserNotFound(id) : Ok(user);
    }

    /// <summary>Creates a user. Email must be unique case-insensitively.</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await users.CreateAsync(request, cancellationToken);

        return result.Outcome switch
        {
            CreateUserOutcome.Created => CreatedAtAction(
                nameof(GetById),
                new { id = result.CreatedUser!.Id },
                result.CreatedUser),
            CreateUserOutcome.DuplicateEmail => DuplicateEmail(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var outcome = await users.UpdateAsync(id, request, cancellationToken);

        return outcome switch
        {
            UpdateUserOutcome.Updated => NoContent(),
            UpdateUserOutcome.NotFound => UserNotFound(id),
            UpdateUserOutcome.DuplicateEmail => DuplicateEmail(),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// Deletes a user. Every task assigned to the user is reassigned to "Deleted User"
    /// in the same operation; no task is deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await users.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : UserNotFound(id);
    }

    private ObjectResult UserNotFound(Guid id) =>
        NotFoundProblem("User not found", $"No user with ID '{id}' exists.");

    private ObjectResult DuplicateEmail() =>
        ConflictProblem("Duplicate email", "Another user already uses this email address.");
}
