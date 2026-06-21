using FluentValidation;
using PRM.Application.Employees;

namespace PRM.Application.Employees;

public class UpdateEmployeeDtoValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeDtoValidator()
    {
        RuleFor(x => x.Department).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(64);
    }
}

public class AssignManagerDtoValidator : AbstractValidator<AssignManagerDto>
{
    public AssignManagerDtoValidator()
    {
        RuleFor(x => x.ManagerId).GreaterThan(0);
    }
}

public class AddResourceProfileSkillDtoValidator : AbstractValidator<AddResourceProfileSkillDto>
{
    public AddResourceProfileSkillDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class UpdateSkillProficiencyDtoValidator : AbstractValidator<UpdateSkillProficiencyDto>
{
    public UpdateSkillProficiencyDtoValidator()
    {
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}
