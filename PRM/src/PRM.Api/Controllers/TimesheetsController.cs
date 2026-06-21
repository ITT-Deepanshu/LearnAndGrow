using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Api.Authorization;
using PRM.Application.Timesheets;
using PRM.Domain.Constants;

namespace PRM.Api.Controllers;

/// <summary>Weekly timesheet submission, history, reminders, and manager team view.</summary>
[ApiController]
[Route("api/v1/timesheets")]
[Authorize]
[Tags("Timesheets")]
public sealed class TimesheetsController(ITimesheetService timesheetService) : ControllerBase
{
    /// <summary>Submit or replace a weekly timesheet with hours and activity tags per project. Resource only.</summary>
    [HttpPost]
    [RequirePermission(RolePermissions.TimesheetsSubmit)]
    public async Task<ActionResult<TimesheetDto>> Submit(
        [FromBody] SubmitTimesheetDto dto,
        CancellationToken cancellationToken)
    {
        var result = await timesheetService.SubmitTimesheetAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetMyForWeek), new { weekStart = result.WeekStart.ToString("yyyy-MM-dd") }, result);
    }

    /// <summary>List all timesheets for the logged-in resource (submitted and missed).</summary>
    [HttpGet("my")]
    [RequirePermission(RolePermissions.TimesheetsViewOwn)]
    public async Task<ActionResult<IReadOnlyList<TimesheetListItemDto>>> ListMy(CancellationToken cancellationToken) =>
        Ok(await timesheetService.ListMyTimesheetsAsync(cancellationToken));

    /// <summary>Get max weekly hours and activity tags for the submit-timesheet form. Resource only.</summary>
    [HttpGet("my/submission-context")]
    [RequirePermission(RolePermissions.TimesheetsSubmit)]
    public async Task<ActionResult<TimesheetSubmissionContextDto>> GetSubmissionContext(CancellationToken cancellationToken) =>
        Ok(await timesheetService.GetSubmissionContextAsync(cancellationToken));

    /// <summary>Check if a missed-timesheet reminder should be shown for the last completed week. Returns 404 when not applicable.</summary>
    [HttpGet("my/reminder")]
    [RequirePermission(RolePermissions.TimesheetsSubmit)]
    public async Task<ActionResult<TimesheetReminderDto>> GetReminder(CancellationToken cancellationToken)
    {
        var reminder = await timesheetService.GetMissedTimesheetReminderAsync(cancellationToken);
        return reminder is null ? NotFound() : Ok(reminder);
    }

    /// <summary>Get timesheet detail for a specific week (yyyy-MM-dd Monday). Resource only.</summary>
    [HttpGet("my/{weekStart}")]
    [RequirePermission(RolePermissions.TimesheetsViewOwn)]
    public async Task<ActionResult<TimesheetDto>> GetMyForWeek(string weekStart, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(weekStart, out var parsedWeekStart))
            return BadRequest("weekStart must be a valid date in yyyy-MM-dd format.");

        var result = await timesheetService.GetMyTimesheetForWeekAsync(parsedWeekStart, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>List team timesheet rows for a week (submitted, missed, not submitted). Manager only.</summary>
    [HttpGet("team")]
    [RequirePermission(RolePermissions.TimesheetsViewTeam)]
    public async Task<ActionResult<IReadOnlyList<TeamTimesheetRowDto>>> ListTeam(
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken) =>
        Ok(await timesheetService.ListTeamTimesheetsAsync(weekStart, cancellationToken));

    /// <summary>Restore timesheet submission access for a direct report after a compliance freeze. Manager only.</summary>
    [HttpPost("team/{resourceProfileId:long}/restore-submission")]
    [RequirePermission(RolePermissions.TimesheetsViewTeam)]
    public async Task<IActionResult> RestoreSubmission(long resourceProfileId, CancellationToken cancellationToken)
    {
        await timesheetService.RestoreTimesheetSubmissionAsync(resourceProfileId, cancellationToken);
        return NoContent();
    }
}
