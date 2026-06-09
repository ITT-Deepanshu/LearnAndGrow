using FluentValidation;
using PRM.Application.Features.Auth.Validators;
using PRM.Application.Features.Users.Commands;
using PRM.Application.Features.Users.Queries;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Users.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(64)
            .Matches("^[a-z0-9._-]+$").WithMessage("Username must contain only lowercase letters, digits, and .-_ characters.");
        RuleFor(x => x.TemporaryPassword).SetValidator(new CreateUserPasswordValidator());
        RuleFor(x => x.Role).IsInEnum().Must(r => r is UserRole.Admin or UserRole.Manager or UserRole.Employee);
    }
}

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.NewPassword).SetValidator(new CreateUserPasswordValidator());
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
