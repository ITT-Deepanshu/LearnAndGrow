using FluentValidation;
using PRM.Application.Features.Employees.Commands;
using PRM.Application.Features.Employees.Queries;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Employees.Validators;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(64);
    }
}

public class DeactivateEmployeeCommandValidator : AbstractValidator<DeactivateEmployeeCommand>
{
    public DeactivateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class AssignManagerCommandValidator : AbstractValidator<AssignManagerCommand>
{
    public AssignManagerCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.ManagerUserId).GreaterThan(0);
    }
}

public class AddEmployeeSkillCommandValidator : AbstractValidator<AddEmployeeSkillCommand>
{
    public AddEmployeeSkillCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class UpdateSkillProficiencyCommandValidator : AbstractValidator<UpdateSkillProficiencyCommand>
{
    public UpdateSkillProficiencyCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.SkillId).GreaterThan(0);
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class RemoveEmployeeSkillCommandValidator : AbstractValidator<RemoveEmployeeSkillCommand>
{
    public RemoveEmployeeSkillCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.SkillId).GreaterThan(0);
    }
}

public class GetEmployeeByIdQueryValidator : AbstractValidator<GetEmployeeByIdQuery>
{
    public GetEmployeeByIdQueryValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class ListEmployeesQueryValidator : AbstractValidator<ListEmployeesQuery>
{
    public ListEmployeesQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.IsDefined(s.Value))
            .WithMessage("Invalid employee status.");
        RuleFor(x => x.Department)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Department));
    }
}
