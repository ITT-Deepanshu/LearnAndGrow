namespace PRM.Application.Timesheets;

public sealed record SubmitTimesheetEntryDto(
    long ProjectId,
    decimal Hours,
    IReadOnlyList<int> TagIds,
    string? CustomText);

public sealed record SubmitTimesheetDto(
    DateOnly WeekStart,
    IReadOnlyList<SubmitTimesheetEntryDto> Entries);

public sealed record TimesheetEntryDto(
    long Id,
    long ProjectId,
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public sealed record TimesheetDto(
    long Id,
    DateOnly WeekStart,
    string Status,
    DateTime? SubmittedAt,
    decimal TotalHours,
    IReadOnlyList<TimesheetEntryDto> Entries);

public sealed record TimesheetListItemDto(
    long Id,
    DateOnly WeekStart,
    string Status,
    decimal TotalHours,
    DateTime? SubmittedAt);

public sealed record TeamTimesheetRowDto(
    string EmployeeName,
    string ProjectName,
    decimal Hours,
    string Status);

public sealed record ActivityTagDto(int Id, string Name, bool IsCustom);

public sealed record TimesheetSubmissionContextDto(
    int MaxWeeklyHours,
    IReadOnlyList<ActivityTagDto> ActivityTags);

public sealed record TimesheetReminderDto(DateOnly WeekStart, string Message);
