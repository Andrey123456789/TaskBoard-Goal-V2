using FluentValidation;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

internal sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(TaskItem.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(TaskItem.DescriptionMaxLength);
    }
}
