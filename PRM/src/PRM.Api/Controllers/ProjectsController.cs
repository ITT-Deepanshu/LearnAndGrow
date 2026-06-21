using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Api.Authorization;
using PRM.Application.Projects;
using PRM.Domain.Constants;

namespace PRM.Api.Controllers;

/// <summary>Projects, milestones, and computed health/risk indicators.</summary>
[ApiController]
[Route("api/v1/projects")]
[Authorize]
[Tags("Projects")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    /// <summary>Create a new project with dates, manager, and story points. Admin only.</summary>
    [HttpPost]
    [RequirePermission(RolePermissions.ProjectsManage)]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await projectService.CreateProjectAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>List all projects with status and health summary. Admin and manager.</summary>
    [HttpGet]
    [RequireAnyPermission(RolePermissions.ProjectsManage, RolePermissions.ProjectsViewOwn)]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await projectService.ListProjectsAsync(cancellationToken));

    /// <summary>Get full project detail: milestones, allocations, and health. Admin and manager.</summary>
    [HttpGet("{id:long}")]
    [RequireAnyPermission(RolePermissions.ProjectsManage, RolePermissions.ProjectsViewOwn)]
    public async Task<ActionResult<ProjectDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await projectService.GetProjectByIdAsync(id, cancellationToken));

    /// <summary>Update project name, description, dates, status, or manager. Admin only.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(RolePermissions.ProjectsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
    {
        await projectService.UpdateProjectAsync(id, dto, cancellationToken);

        return NoContent();
    }

    /// <summary>Add a milestone to a project. Admin only.</summary>
    [HttpPost("{id:long}/milestones")]
    [RequirePermission(RolePermissions.ProjectsManage)]
    public async Task<ActionResult<MilestoneDto>> AddMilestone(
        long id,
        [FromBody] AddMilestoneDto dto,
        CancellationToken cancellationToken)
    {
        var result = await projectService.AddMilestoneAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(ListMilestones), new { id }, result);
    }

    /// <summary>List all milestones for a project. Admin and manager.</summary>
    [HttpGet("{id:long}/milestones")]
    [RequireAnyPermission(RolePermissions.ProjectsManage, RolePermissions.ProjectsViewOwn)]
    public async Task<ActionResult<IReadOnlyList<MilestoneDto>>> ListMilestones(long id, CancellationToken cancellationToken) =>
        Ok(await projectService.ListMilestonesAsync(id, cancellationToken));

    /// <summary>Get project health (green/yellow/red) and risk flags from milestones and timesheets. Admin and manager.</summary>
    [HttpGet("{id:long}/health")]
    [RequireAnyPermission(RolePermissions.ProjectsManage, RolePermissions.ProjectsViewOwn)]
    public async Task<ActionResult<ProjectHealthDto>> GetHealth(long id, CancellationToken cancellationToken) =>
        Ok(await projectService.GetProjectHealthAsync(id, cancellationToken));

    /// <summary>Update milestone status (e.g. NotStarted, InProgress, Completed). Admin only.</summary>
    [HttpPut("{id:long}/milestones/{milestoneId:long}/status")]
    [RequirePermission(RolePermissions.ProjectsManage)]
    public async Task<IActionResult> UpdateMilestoneStatus(
        long id,
        long milestoneId,
        [FromBody] UpdateMilestoneStatusDto dto,
        CancellationToken cancellationToken)
    {
        await projectService.UpdateMilestoneStatusAsync(id, milestoneId, dto.Status, cancellationToken);
        return NoContent();
    }
}
