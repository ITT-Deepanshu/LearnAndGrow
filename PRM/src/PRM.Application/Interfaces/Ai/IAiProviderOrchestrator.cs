namespace PRM.Application.Interfaces.Ai;

public interface IAiProviderOrchestrator
{
    Task<SkillMatchAiResponse> MatchSkillsAsync(SkillMatchAiRequest request, CancellationToken cancellationToken = default);
    Task<RiskSummaryAiResponse> SummarizeRiskAsync(RiskSummaryAiRequest request, CancellationToken cancellationToken = default);
}
