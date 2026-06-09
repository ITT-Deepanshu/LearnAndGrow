using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Allocations.Commands;
using PRM.Application.Features.Allocations.Dtos;
using PRM.Application.Features.Allocations.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/allocations")]
[Authorize]
public sealed class AllocationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<AllocationDto>> Create([FromBody] CreateAllocationDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateAllocationCommand(
            dto.ProjectId,
            dto.EmployeeId,
            dto.UtilisationPercentage,
            dto.FromDate,
            dto.ToDate), cancellationToken);

        return CreatedAtAction(nameof(ListByProject), new { projectId = result.ProjectId }, result);
    }

    [HttpPost("{id:long}/end")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> End(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new EndAllocationCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AllocationListItemDto>>> List(
        [FromQuery] long? employeeId,
        [FromQuery] long? projectId,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListAllocationsQuery(employeeId, projectId), cancellationToken));

    [HttpGet("by-project/{projectId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> ListByProject(long projectId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListAllocationsByProjectQuery(projectId), cancellationToken));

    [HttpGet("by-employee/{employeeId:long}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> ListByEmployee(long employeeId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListAllocationsByEmployeeQuery(employeeId), cancellationToken));
}
