using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Ai;

namespace PRM.Api.Controllers;

/// <summary>Gemma-powered resource matching and project risk summaries. Manager only.</summary>
[ApiController]
[Route("api/v1/ai")]
[Authorize(Roles = "manager")]
[Tags("AI")]
public sealed class AiController(IAiService aiService) : ControllerBase
{
    /// <summary>Find best-fit resources for a project based on a natural-language requirement.</summary>
    [HttpGet("skill-match")]
    public async Task<ActionResult<SkillMatchResultDto>> SkillMatch(
        [FromQuery] long projectId,
        [FromQuery] string requirement,
        CancellationToken cancellationToken) =>
        Ok(await aiService.FindResourcesAsync(projectId, requirement, cancellationToken));

    /// <summary>
    /// Parse a natural-language team request, match roles against bench employees, and return assignments and gaps.
    /// Employee data is loaded from the database first; the LLM only sees the provided snapshot.
    /// </summary>
    [HttpPost("team-skill-match")]
    public async Task<ActionResult<TeamSkillMatchResultDto>> TeamSkillMatch(
        [FromBody] TeamSkillMatchRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await aiService.MatchTeamAsync(request.Requirement, cancellationToken));

    /// <summary>Generate an AI risk summary for a project using milestones, allocations, and timesheet data.</summary>
    [HttpGet("risk-summary/{projectId:long}")]
    public async Task<ActionResult<RiskSummaryDto>> RiskSummary(long projectId, CancellationToken cancellationToken) =>
        Ok(await aiService.GetProjectRiskSummaryAsync(projectId, cancellationToken));
}
