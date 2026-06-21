using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Api.Authorization;
using PRM.Application.Employees;
using PRM.Domain.Constants;
using PRM.Domain.Enums;

namespace PRM.Api.Controllers;

/// <summary>Resource profiles (employees): directory, skills, manager assignment, and status.</summary>
[ApiController]
[Route("api/v1/employees")]
[Authorize]
[Tags("Employees")]
public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    /// <summary>List employees with optional status and department filters. Admin and manager.</summary>
    [HttpGet]
    [RequireAnyPermission(RolePermissions.ResourceProfilesManage, RolePermissions.DashboardView)]
    public async Task<ActionResult<IReadOnlyList<EmployeeListItemDto>>> List(
        [FromQuery] ResourceProfileStatus? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken) =>
        Ok(await employeeService.ListEmployeesAsync(status, department, cancellationToken));

    /// <summary>Get employee detail including skills and manager. Admin and manager.</summary>
    [HttpGet("{id:long}")]
    [RequireAnyPermission(RolePermissions.ResourceProfilesManage, RolePermissions.DashboardView)]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await employeeService.GetEmployeeByIdAsync(id, cancellationToken));

    /// <summary>Update department, designation, and related profile fields. Admin only.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeDto dto, CancellationToken cancellationToken)
    {
        await employeeService.UpdateEmployeeAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>Mark an employee as inactive (bench/offboarding). Admin only.</summary>
    [HttpPost("{id:long}/deactivate")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        await employeeService.DeactivateEmployeeAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Reactivate an inactive employee. Admin only.</summary>
    [HttpPost("{id:long}/reactivate")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> Reactivate(long id, CancellationToken cancellationToken)
    {
        await employeeService.ReactivateEmployeeAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Assign or change the reporting manager for an employee. Admin only.</summary>
    [HttpPost("{id:long}/assign-manager")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> AssignManager(long id, [FromBody] AssignManagerDto dto, CancellationToken cancellationToken)
    {
        await employeeService.AssignManagerAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>Add a skill to an employee's profile. Admin only.</summary>
    [HttpPost("{id:long}/skills")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<ActionResult<ResourceProfileSkillDto>> AddSkill(
        long id,
        [FromBody] AddResourceProfileSkillDto dto,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.AddSkillAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    /// <summary>Update proficiency level for an existing employee skill. Admin only.</summary>
    [HttpPut("{id:long}/skills/{skillId:long}")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> UpdateSkillProficiency(
        long id,
        long skillId,
        [FromBody] UpdateSkillProficiencyDto dto,
        CancellationToken cancellationToken)
    {
        await employeeService.UpdateSkillProficiencyAsync(id, skillId, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>Remove a skill from an employee's profile. Admin only.</summary>
    [HttpDelete("{id:long}/skills/{skillId:long}")]
    [RequirePermission(RolePermissions.ResourceProfilesManage)]
    public async Task<IActionResult> RemoveSkill(long id, long skillId, CancellationToken cancellationToken)
    {
        await employeeService.RemoveSkillAsync(id, skillId, cancellationToken);
        return NoContent();
    }
}
