using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.Services.Tasks;

public sealed record CreateTaskResult(CreateTaskOutcome Outcome, TaskDetailsDto? CreatedTask);
