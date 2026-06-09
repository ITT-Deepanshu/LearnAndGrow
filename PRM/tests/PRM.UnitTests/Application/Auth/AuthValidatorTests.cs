using FluentAssertions;
using FluentValidation.TestHelper;
using PRM.Application.Features.Auth.Commands;
using PRM.Application.Features.Auth.Validators;

namespace PRM.UnitTests.Application.Auth;

public class AuthValidatorTests
{
    [Fact]
    public void LoginCommandValidator_RejectsEmptyUsername()
    {
        var validator = new LoginCommandValidator();
        var result = validator.TestValidate(new LoginCommand("", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ChangePasswordCommandValidator_RejectsWeakPassword()
    {
        var validator = new ChangePasswordCommandValidator();
        var result = validator.TestValidate(new ChangePasswordCommand("old", "weak", "weakpass"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ChangePasswordCommandValidator_AcceptsStrongPassword()
    {
        var validator = new ChangePasswordCommandValidator();
        var result = validator.TestValidate(new ChangePasswordCommand("old", "StrongPass1", "StrongPass1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateUserPasswordValidator_RequiresUppercaseAndDigit()
    {
        var validator = new CreateUserPasswordValidator();
        validator.Validate("short").IsValid.Should().BeFalse();
        validator.Validate("LongEnough1").IsValid.Should().BeTrue();
    }
}
