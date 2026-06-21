using PRM.Application.Timesheets;
using PRM.Domain.Entities;

namespace PRM.Application.Timesheets;

internal static class TimesheetMappings
{
    internal static TimesheetDto ToDto(Timesheet timesheet) =>
        new(
            timesheet.Id,
            timesheet.WeekStart,
            timesheet.Status.ToString(),
            timesheet.SubmittedAt,
            timesheet.TotalHours,
            timesheet.Entries.Select(ToEntryDto).ToList());

    internal static TimesheetEntryDto ToEntryDto(TimesheetEntry entry) =>
        new(
            entry.Id,
            entry.ProjectId,
            entry.Project?.Name ?? string.Empty,
            entry.Hours,
            entry.ActivityTags.Select(t => t.Name).ToList());

    internal static TimesheetListItemDto ToListItemDto(Timesheet timesheet) =>
        new(
            timesheet.Id,
            timesheet.WeekStart,
            timesheet.Status.ToString(),
            timesheet.TotalHours,
            timesheet.SubmittedAt);
}
