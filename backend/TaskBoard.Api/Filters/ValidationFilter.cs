using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace TaskBoard.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator for each action argument and returns the
/// standard 400 validation problem response when any rule fails.
/// </summary>
internal sealed class ValidationFilter(IOptions<ApiBehaviorOptions> apiBehaviorOptions) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = apiBehaviorOptions.Value.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }
}
