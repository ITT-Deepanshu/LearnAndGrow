using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PRM.Domain.Exceptions;

namespace PRM.Api.Filters;

/// <summary>
/// Runs FluentValidation validators for action parameters before the controller executes.
/// </summary>
public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null || !ShouldValidate(argument.GetType()))
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
                throw new PRM.Domain.Exceptions.ValidationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
        }

        await next();
    }

    /// <summary>
    /// Route/query primitives (e.g. weekStart dates) must not run DTO validators such as
    /// <see cref="PRM.Application.Auth.CreateUserPasswordValidator"/> registered for <see cref="string"/>.
    /// </summary>
    private static bool ShouldValidate(Type type)
    {
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            return false;

        return type != typeof(decimal)
            && type != typeof(DateOnly)
            && type != typeof(DateTime)
            && type != typeof(DateTimeOffset)
            && type != typeof(TimeOnly)
            && type != typeof(Guid);
    }
}
