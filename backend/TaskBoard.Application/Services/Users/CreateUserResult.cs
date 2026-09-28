using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Application.Services.Users;

public sealed record CreateUserResult(CreateUserOutcome Outcome, UserDto? CreatedUser);
