using PRM.Application.Ai;

namespace PRM.Application.Ai;

public interface IAiService
{
    Task<SkillMatchResultDto> FindResourcesAsync(long projectId, string requirement, CancellationToken cancellationToken = default);
    Task<TeamSkillMatchResultDto> MatchTeamAsync(string requirement, CancellationToken cancellationToken = default);
    Task<RiskSummaryDto> GetProjectRiskSummaryAsync(long projectId, CancellationToken cancellationToken = default);
}
