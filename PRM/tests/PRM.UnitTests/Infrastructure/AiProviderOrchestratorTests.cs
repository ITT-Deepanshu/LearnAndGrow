using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PRM.Application.Interfaces.Ai;

namespace PRM.UnitTests.Infrastructure;

public class AiProviderOrchestratorTests
{
    [Fact]
    public async Task MatchSkillsAsync_FallsBackToSecondaryProviderWhenPrimaryFails()
    {
        var request = new SkillMatchAiRequest("need java dev", "Alpha", []);
        var expected = new SkillMatchAiResponse([new RankedCandidateAiResult(1, "Anil", "Good fit", 50)]);

        var primary = new FakeAiProvider("Gemini", _ => throw new InvalidOperationException("Gemini down"));
        var fallback = new FakeAiProvider("Grok", _ => Task.FromResult(expected));

        var orchestrator = new FallbackOrchestrator(primary, fallback, NullLogger.Instance);

        var result = await orchestrator.MatchSkillsAsync(request, CancellationToken.None);

        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].Name.Should().Be("Anil");
        fallback.CallCount.Should().Be(1);
    }

    private sealed class FakeAiProvider(string name, Func<SkillMatchAiRequest, Task<SkillMatchAiResponse>> match) : IAiProvider
    {
        public string ProviderName { get; } = name;
        public int CallCount { get; private set; }

        public Task<SkillMatchAiResponse> MatchSkillsAsync(SkillMatchAiRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return match(request);
        }

        public Task<RiskSummaryAiResponse> SummarizeRiskAsync(RiskSummaryAiRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RiskSummaryAiResponse("Risk paragraph"));
    }

    private sealed class FallbackOrchestrator(FakeAiProvider primary, FakeAiProvider fallback, ILogger logger)
    {
        public async Task<SkillMatchAiResponse> MatchSkillsAsync(SkillMatchAiRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return await primary.MatchSkillsAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Primary failed, using fallback");
                return await fallback.MatchSkillsAsync(request, cancellationToken);
            }
        }
    }
}
