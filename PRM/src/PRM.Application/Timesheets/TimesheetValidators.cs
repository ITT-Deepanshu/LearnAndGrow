using FluentValidation;
using PRM.Application.Timesheets;
using PRM.Application.Interfaces.Common;
using PRM.Domain.Helpers;

namespace PRM.Application.Timesheets;

public sealed class SubmitTimesheetDtoValidator : AbstractValidator<SubmitTimesheetDto>
{
    public SubmitTimesheetDtoValidator(IClock clock)
    {
        RuleFor(x => x.WeekStart)
            .Must(WeekHelper.IsMonday)
            .WithMessage("Week start must be a Monday.");

        RuleFor(x => x.WeekStart)
            .Must(weekStart => !WeekHelper.IsFutureWeek(weekStart, clock.Today))
            .WithMessage("Week start cannot be in the future.");

        RuleFor(x => x.Entries)
            .NotEmpty()
            .WithMessage("At least one timesheet entry is required.");

        RuleForEach(x => x.Entries).SetValidator(new SubmitTimesheetEntryDtoValidator());

        RuleFor(x => x.Entries)
            .Must(entries => entries.Select(e => e.ProjectId).Distinct().Count() == entries.Count)
            .WithMessage("Each project may appear only once per timesheet.");
    }
}

public sealed class SubmitTimesheetEntryDtoValidator : AbstractValidator<SubmitTimesheetEntryDto>
{
    public SubmitTimesheetEntryDtoValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(168);
        RuleFor(x => x.TagIds).NotEmpty().WithMessage("At least one activity tag is required.");
    }
}
