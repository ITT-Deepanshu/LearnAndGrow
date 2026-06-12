using FluentValidation;
using PRM.Application.Projects;

namespace PRM.Application.Projects;

public sealed class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.ManagerId).GreaterThan(0);
        RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public sealed class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.ManagerId).GreaterThan(0);
        RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public sealed class AddMilestoneDtoValidator : AbstractValidator<AddMilestoneDto>
{
    public AddMilestoneDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.StoryPoints).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateMilestoneStatusDtoValidator : AbstractValidator<UpdateMilestoneStatusDto>
{
    public UpdateMilestoneStatusDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
