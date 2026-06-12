using FluentValidation;
using PRM.Application.Allocations;

namespace PRM.Application.Allocations;

public sealed class CreateAllocationDtoValidator : AbstractValidator<CreateAllocationDto>
{
    public CreateAllocationDtoValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.ResourceProfileId).GreaterThan(0);
        RuleFor(x => x.UtilisationPercentage).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
