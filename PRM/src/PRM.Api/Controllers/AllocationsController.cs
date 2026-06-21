using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Api.Authorization;
using PRM.Application.Allocations;
using PRM.Domain.Constants;

namespace PRM.Api.Controllers;

/// <summary>Assign resources to projects, view allocations, and end assignments.</summary>
[ApiController]
[Route("api/v1/allocations")]
[Authorize]
[Tags("Allocations")]
public sealed class AllocationsController(IAllocationService allocationService) : ControllerBase
{
    /// <summary>Create a resource-to-project allocation with utilisation % and date range. Manager only.</summary>
    [HttpPost]
    [RequirePermission(RolePermissions.AllocationsManage)]
    public async Task<ActionResult<AllocationDto>> Create([FromBody] CreateAllocationDto dto, CancellationToken cancellationToken)
    {
        var result = await allocationService.CreateAllocationAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(ListByProject), new { projectId = result.ProjectId }, result);
    }

    /// <summary>End an active allocation as of today. Manager only.</summary>
    [HttpPost("{id:long}/end")]
    [RequirePermission(RolePermissions.AllocationsManage)]
    public async Task<IActionResult> End(long id, CancellationToken cancellationToken)
    {
        await allocationService.EndAllocationAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>List active allocations with optional employee or project filters. Admin only.</summary>
    [HttpGet]
    [RequirePermission(RolePermissions.AllocationsViewAll)]
    public async Task<ActionResult<IReadOnlyList<AllocationListItemDto>>> List(
        [FromQuery] long? employeeId,
        [FromQuery] long? projectId,
        CancellationToken cancellationToken) =>
        Ok(await allocationService.ListAllocationsAsync(employeeId, projectId, cancellationToken));

    /// <summary>List active allocations on a project. Admin and manager.</summary>
    [HttpGet("by-project/{projectId:long}")]
    [RequireAnyPermission(RolePermissions.AllocationsViewAll, RolePermissions.AllocationsManage)]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> ListByProject(long projectId, CancellationToken cancellationToken) =>
        Ok(await allocationService.ListAllocationsByProjectAsync(projectId, cancellationToken));

    /// <summary>List the logged-in resource's own active allocations.</summary>
    [HttpGet("my")]
    [RequirePermission(RolePermissions.AllocationsViewOwn)]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> ListMy(CancellationToken cancellationToken) =>
        Ok(await allocationService.ListMyAllocationsAsync(cancellationToken));

    /// <summary>List active allocations for an employee. Resource may only query their own profile.</summary>
    [HttpGet("by-employee/{employeeId:long}")]
    [RequireAnyPermission(
        RolePermissions.AllocationsViewAll,
        RolePermissions.AllocationsManage,
        RolePermissions.AllocationsViewOwn)]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> ListByEmployee(long employeeId, CancellationToken cancellationToken) =>
        Ok(await allocationService.ListAllocationsByEmployeeAsync(employeeId, cancellationToken));
}
