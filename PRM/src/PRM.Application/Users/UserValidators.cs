using FluentValidation;
using PRM.Application.Auth;
using PRM.Application.Users;

namespace PRM.Application.Users;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(64)
            .Matches("^[a-z0-9._-]+$").WithMessage("Username must contain only lowercase letters, digits, and .-_ characters.");
        RuleFor(x => x.TemporaryPassword).SetValidator(new CreateUserPasswordValidator());
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase)
                || r.Equals("manager", StringComparison.OrdinalIgnoreCase)
                || r.Equals("resource", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be Admin, Manager, or Resource.");
    }
}

public class ResetUserPasswordDtoValidator : AbstractValidator<ResetUserPasswordDto>
{
    public ResetUserPasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new CreateUserPasswordValidator());
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}
