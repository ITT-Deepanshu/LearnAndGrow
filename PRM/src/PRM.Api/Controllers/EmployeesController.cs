using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Employees.Commands;
using PRM.Application.Features.Employees.Dtos;
using PRM.Application.Features.Employees.Queries;
using PRM.Domain.Enums;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public sealed class EmployeesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IReadOnlyList<EmployeeListItemDto>>> List(
        [FromQuery] EmployeeStatus? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListEmployeesQuery(status, department), cancellationToken));

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetEmployeeByIdQuery(id), cancellationToken));

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateEmployeeCommand(id, dto.Department, dto.Designation), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateEmployeeCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/assign-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignManager(long id, [FromBody] AssignManagerDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignManagerCommand(id, dto.ManagerId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/skills")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmployeeSkillDto>> AddSkill(
        long id,
        [FromBody] AddEmployeeSkillDto dto,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddEmployeeSkillCommand(id, dto.Name, dto.Category, dto.Proficiency), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    [HttpPut("{id:long}/skills/{skillId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSkillProficiency(
        long id,
        long skillId,
        [FromBody] UpdateSkillProficiencyDto dto,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateSkillProficiencyCommand(id, skillId, dto.Proficiency), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}/skills/{skillId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveSkill(long id, long skillId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveEmployeeSkillCommand(id, skillId), cancellationToken);
        return NoContent();
    }
}
