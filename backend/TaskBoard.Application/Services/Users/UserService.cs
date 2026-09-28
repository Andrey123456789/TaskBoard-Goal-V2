using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Services.Users;

internal sealed class UserService(
    IUserRepository users,
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<UserService> logger)
    : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var ordinaryUsers = await users.ListOrdinaryAsync(cancellationToken);

        return [.. ordinaryUsers.Select(ToDto)];
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await FindOrdinaryUserAsync(id, cancellationToken);

        return user is null ? null : ToDto(user);
    }

    public async Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (await users.EmailExistsAsync(request.Email, excludedUserId: null, cancellationToken))
        {
            return new CreateUserResult(CreateUserOutcome.DuplicateEmail, null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            CreatedAt = clock.GetUtcNow(),
        };

        users.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return new CreateUserResult(CreateUserOutcome.DuplicateEmail, null);
        }

        return new CreateUserResult(CreateUserOutcome.Created, ToDto(user));
    }

    public async Task<UpdateUserOutcome> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindOrdinaryUserAsync(id, cancellationToken);

        if (user is null)
        {
            return UpdateUserOutcome.NotFound;
        }

        if (await users.EmailExistsAsync(request.Email, excludedUserId: id, cancellationToken))
        {
            return UpdateUserOutcome.DuplicateEmail;
        }

        user.Name = request.Name;
        user.Email = request.Email;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return UpdateUserOutcome.DuplicateEmail;
        }

        return UpdateUserOutcome.Updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await FindOrdinaryUserAsync(id, cancellationToken);

        if (user is null)
        {
            return false;
        }

        // System reassignment: applies to tasks in every status, including Completed and Closed,
        // and is a core-state change, so UpdatedAt changes.
        var assignedTasks = await tasks.GetAssignedToUserAsync(id, cancellationToken);
        var now = clock.GetUtcNow();

        foreach (var task in assignedTasks)
        {
            task.AssigneeId = User.DeletedUserId;
            task.UpdatedAt = now;
        }

        users.Remove(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted; {TaskCount} task(s) reassigned to Deleted User",
            id,
            assignedTasks.Count);

        return true;
    }

    private async Task<User?> FindOrdinaryUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == User.DeletedUserId)
        {
            return null;
        }

        return await users.GetByIdAsync(id, cancellationToken);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.CreatedAt);
}
