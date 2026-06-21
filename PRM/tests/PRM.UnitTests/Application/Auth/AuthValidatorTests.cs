using FluentAssertions;
using FluentValidation.TestHelper;
using PRM.Application.Auth;

namespace PRM.UnitTests.Application.Auth;

public class AuthValidatorTests
{
    [Fact]
    public void LoginDtoValidator_RejectsEmptyUsername()
    {
        var validator = new LoginDtoValidator();
        var result = validator.TestValidate(new LoginDto("", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ChangePasswordDtoValidator_RejectsWeakPassword()
    {
        var validator = new ChangePasswordDtoValidator();
        var result = validator.TestValidate(new ChangePasswordDto("old", "weak", "weakpass"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ChangePasswordDtoValidator_AcceptsStrongPassword()
    {
        var validator = new ChangePasswordDtoValidator();
        var result = validator.TestValidate(new ChangePasswordDto("old", "StrongPass1", "StrongPass1"));
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
