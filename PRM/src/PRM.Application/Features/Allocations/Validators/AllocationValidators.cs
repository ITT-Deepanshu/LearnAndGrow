using FluentValidation;
using PRM.Application.Features.Allocations.Commands;

namespace PRM.Application.Features.Allocations.Validators;

public sealed class CreateAllocationCommandValidator : AbstractValidator<CreateAllocationCommand>
{
    public CreateAllocationCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.UtilisationPercentage).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}

public sealed class EndAllocationCommandValidator : AbstractValidator<EndAllocationCommand>
{
    public EndAllocationCommandValidator()
    {
        RuleFor(x => x.AllocationId).GreaterThan(0);
    }
}
