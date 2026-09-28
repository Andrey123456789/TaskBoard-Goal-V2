using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Application.Services.Users;

/// <summary>
/// Manages ordinary users. The system <c>Deleted User</c> is never listed, returned,
/// edited, or deleted through this service.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateUserResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UpdateUserOutcome> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the user and reassigns all of its tasks to <c>Deleted User</c> in one commit.
    /// </summary>
    /// <returns><c>false</c> when no ordinary user with the ID exists.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
