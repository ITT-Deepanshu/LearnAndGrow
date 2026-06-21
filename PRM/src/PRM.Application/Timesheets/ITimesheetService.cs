using PRM.Application.Timesheets;

namespace PRM.Application.Timesheets;

public interface ITimesheetService
{
    Task<TimesheetDto> SubmitTimesheetAsync(SubmitTimesheetDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetListItemDto>> ListMyTimesheetsAsync(CancellationToken cancellationToken = default);
    Task<TimesheetDto?> GetMyTimesheetForWeekAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamTimesheetRowDto>> ListTeamTimesheetsAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<TimesheetSubmissionContextDto> GetSubmissionContextAsync(CancellationToken cancellationToken = default);
    Task<TimesheetReminderDto?> GetMissedTimesheetReminderAsync(CancellationToken cancellationToken = default);
    Task RestoreTimesheetSubmissionAsync(long resourceProfileId, CancellationToken cancellationToken = default);
}
