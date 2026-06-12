using PRM.Application.Interfaces.Ai;

namespace PRM.Infrastructure.Ai;

public sealed class AiProviderOrchestrator(IAiProvider provider) : IAiProviderOrchestrator
{
    public Task<SkillMatchAiResponse> MatchSkillsAsync(
        SkillMatchAiRequest request,
        CancellationToken cancellationToken = default) =>
        provider.MatchSkillsAsync(request, cancellationToken);

    public Task<TeamSkillMatchAiResponse> MatchTeamAsync(
        TeamSkillMatchAiRequest request,
        CancellationToken cancellationToken = default) =>
        provider.MatchTeamAsync(request, cancellationToken);

    public Task<RiskSummaryAiResponse> SummarizeRiskAsync(
        RiskSummaryAiRequest request,
        CancellationToken cancellationToken = default) =>
        provider.SummarizeRiskAsync(request, cancellationToken);
}
