using FluentValidation;
using PRM.Application.Features.Projects.Commands;

namespace PRM.Application.Features.Projects.Validators;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.ManagerId).GreaterThan(0);
        RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.ManagerId).GreaterThan(0);
        RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public sealed class AddMilestoneCommandValidator : AbstractValidator<AddMilestoneCommand>
{
    public AddMilestoneCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.StoryPoints).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateMilestoneStatusCommandValidator : AbstractValidator<UpdateMilestoneStatusCommand>
{
    public UpdateMilestoneStatusCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.MilestoneId).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}
