using FluentValidation;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

internal sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Project.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Project.DescriptionMaxLength);
    }
}
