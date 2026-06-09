using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Ai.Dtos;
using PRM.Application.Features.Ai.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize(Roles = "Manager")]
public sealed class AiController(IMediator mediator) : ControllerBase
{
    [HttpGet("skill-match")]
    public async Task<ActionResult<SkillMatchResultDto>> SkillMatch(
        [FromQuery] long projectId,
        [FromQuery] string requirement,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new FindResourcesUsingAIQuery(projectId, requirement), cancellationToken));

    [HttpGet("risk-summary/{projectId:long}")]
    public async Task<ActionResult<RiskSummaryDto>> RiskSummary(long projectId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetProjectRiskSummaryQuery(projectId), cancellationToken));
}
