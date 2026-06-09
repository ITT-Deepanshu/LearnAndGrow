using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Timesheets.Commands;
using PRM.Application.Features.Timesheets.Dtos;
using PRM.Application.Features.Timesheets.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/timesheets")]
[Authorize]
public sealed class TimesheetsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<TimesheetDto>> Submit(
        [FromBody] SubmitTimesheetDto dto,
        CancellationToken cancellationToken)
    {
        var entries = dto.Entries
            .Select(e => new SubmitTimesheetEntryCommand(e.ProjectId, e.Hours, e.TagIds, e.CustomText))
            .ToList();

        var result = await mediator.Send(new SubmitTimesheetCommand(dto.WeekStart, entries), cancellationToken);
        return CreatedAtAction(nameof(GetMyForWeek), new { weekStart = result.WeekStart.ToString("yyyy-MM-dd") }, result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<IReadOnlyList<TimesheetListItemDto>>> ListMy(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListMyTimesheetsQuery(), cancellationToken));

    [HttpGet("my/{weekStart}")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<TimesheetDto>> GetMyForWeek(string weekStart, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(weekStart, out var parsedWeekStart))
            return BadRequest("weekStart must be a valid date in yyyy-MM-dd format.");

        var result = await mediator.Send(new GetMyTimesheetForWeekQuery(parsedWeekStart), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IReadOnlyList<TeamTimesheetRowDto>>> ListTeam(
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListTeamTimesheetsQuery(weekStart), cancellationToken));
}
