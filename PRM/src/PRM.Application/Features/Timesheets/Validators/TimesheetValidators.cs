using FluentValidation;
using PRM.Application.Features.Timesheets.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Timesheets.Validators;

public sealed class SubmitTimesheetCommandValidator : AbstractValidator<SubmitTimesheetCommand>
{
    public SubmitTimesheetCommandValidator(IClock clock)
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

        RuleForEach(x => x.Entries).SetValidator(new SubmitTimesheetEntryCommandValidator());

        RuleFor(x => x.Entries)
            .Must(entries => entries.Select(e => e.ProjectId).Distinct().Count() == entries.Count)
            .WithMessage("Each project may appear only once per timesheet.");
    }
}

public sealed class SubmitTimesheetEntryCommandValidator : AbstractValidator<SubmitTimesheetEntryCommand>
{
    public SubmitTimesheetEntryCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(168);
        RuleFor(x => x.TagIds).NotEmpty().WithMessage("At least one activity tag is required.");
    }
}

public sealed class GetMyTimesheetForWeekQueryValidator : AbstractValidator<Queries.GetMyTimesheetForWeekQuery>
{
    public GetMyTimesheetForWeekQueryValidator()
    {
        RuleFor(x => x.WeekStart)
            .Must(WeekHelper.IsMonday)
            .WithMessage("Week start must be a Monday.");
    }
}

public sealed class ListTeamTimesheetsQueryValidator : AbstractValidator<Queries.ListTeamTimesheetsQuery>
{
    public ListTeamTimesheetsQueryValidator()
    {
        RuleFor(x => x.WeekStart)
            .Must(WeekHelper.IsMonday)
            .WithMessage("Week start must be a Monday.");
    }
}
