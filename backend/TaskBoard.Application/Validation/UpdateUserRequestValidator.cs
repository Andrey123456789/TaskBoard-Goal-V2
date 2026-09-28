using FluentValidation;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

internal sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(User.NameMaxLength);
        RuleFor(x => x.Email).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(User.EmailMaxLength).EmailAddress();
    }
}
