using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class AiApi(PrmHttpClient http)
{
    public Task<SkillMatchResult> SkillMatchAsync(long projectId, string requirement, CancellationToken ct = default)
    {
        var query = PrmHttpClient.BuildQuery(("projectId", projectId.ToString()), ("requirement", requirement));
        return http.GetAsync<SkillMatchResult>($"api/v1/ai/skill-match{query}", ct);
    }

    public Task<RiskSummary> RiskSummaryAsync(long projectId, CancellationToken ct = default) =>
        http.GetAsync<RiskSummary>($"api/v1/ai/risk-summary/{projectId}", ct);

    public Task<TeamSkillMatchResult> TeamSkillMatchAsync(string requirement, CancellationToken ct = default) =>
        http.PostAsync<TeamSkillMatchRequest, TeamSkillMatchResult>(
            "api/v1/ai/team-skill-match",
            new TeamSkillMatchRequest(requirement),
            ct);
}
