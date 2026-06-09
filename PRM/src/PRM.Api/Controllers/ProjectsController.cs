using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Projects.Commands;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Features.Projects.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProjectCommand(
            dto.Name,
            dto.Description,
            dto.StartDate,
            dto.EndDate,
            dto.Status,
            dto.ManagerId,
            dto.TotalStoryPoints), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListProjectsQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetProjectByIdQuery(id), cancellationToken));

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProjectCommand(
            id,
            dto.Name,
            dto.Description,
            dto.StartDate,
            dto.EndDate,
            dto.Status,
            dto.ManagerId,
            dto.TotalStoryPoints), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:long}/milestones")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MilestoneDto>> AddMilestone(
        long id,
        [FromBody] AddMilestoneDto dto,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddMilestoneCommand(id, dto.Title, dto.DueDate, dto.StoryPoints), cancellationToken);
        return CreatedAtAction(nameof(ListMilestones), new { id }, result);
    }

    [HttpGet("{id:long}/milestones")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IReadOnlyList<MilestoneDto>>> ListMilestones(long id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListMilestonesQuery(id), cancellationToken));

    [HttpPut("{id:long}/milestones/{milestoneId:long}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMilestoneStatus(
        long id,
        long milestoneId,
        [FromBody] UpdateMilestoneStatusDto dto,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateMilestoneStatusCommand(id, milestoneId, dto.Status), cancellationToken);
        return NoContent();
    }
}
