using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Ai;

namespace PRM.Infrastructure.Ai;

public sealed class AiProviderOrchestrator(
    GeminiProvider geminiProvider,
    GrokProvider grokProvider,
    ILogger<AiProviderOrchestrator> logger) : IAiProviderOrchestrator
{
    public async Task<SkillMatchAiResponse> MatchSkillsAsync(
        SkillMatchAiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await geminiProvider.MatchSkillsAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Gemini provider failed for skill match; falling back to Grok ({Provider})",
                grokProvider.ProviderName);

            return await grokProvider.MatchSkillsAsync(request, cancellationToken);
        }
    }

    public async Task<RiskSummaryAiResponse> SummarizeRiskAsync(
        RiskSummaryAiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await geminiProvider.SummarizeRiskAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Gemini provider failed for risk summary; falling back to Grok ({Provider})",
                grokProvider.ProviderName);

            return await grokProvider.SummarizeRiskAsync(request, cancellationToken);
        }
    }
}
