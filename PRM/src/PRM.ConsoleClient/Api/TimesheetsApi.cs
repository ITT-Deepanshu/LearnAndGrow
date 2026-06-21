using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class TimesheetsApi(PrmHttpClient http)
{
    public Task<Timesheet> SubmitAsync(SubmitTimesheetRequest request, CancellationToken ct = default) =>
        http.PostAsync<SubmitTimesheetRequest, Timesheet>("api/v1/timesheets", request, ct);

    public Task<IReadOnlyList<TimesheetListItem>> ListMyAsync(CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<TimesheetListItem>>("api/v1/timesheets/my", ct);

    public Task<TimesheetSubmissionContext> GetSubmissionContextAsync(CancellationToken ct = default) =>
        http.GetAsync<TimesheetSubmissionContext>("api/v1/timesheets/my/submission-context", ct);

    public Task<TimesheetReminder?> GetReminderAsync(CancellationToken ct = default) =>
        http.GetOptionalAsync<TimesheetReminder>("api/v1/timesheets/my/reminder", ct);

    public Task<Timesheet?> GetMyForWeekAsync(DateOnly weekStart, CancellationToken ct = default)
    {
        var key = weekStart.ToString("yyyy-MM-dd");
        return http.GetOptionalAsync<Timesheet>($"api/v1/timesheets/my/{key}", ct);
    }

    public Task<IReadOnlyList<TeamTimesheetRow>> ListTeamAsync(DateOnly weekStart, CancellationToken ct = default)
    {
        var query = PrmHttpClient.BuildQuery(("weekStart", weekStart.ToString("yyyy-MM-dd")));
        return http.GetAsync<IReadOnlyList<TeamTimesheetRow>>($"api/v1/timesheets/team{query}", ct);
    }

    public Task RestoreSubmissionAsync(long resourceProfileId, CancellationToken ct = default) =>
        http.PostAsync($"api/v1/timesheets/team/{resourceProfileId}/restore-submission", new { }, ct);
}
