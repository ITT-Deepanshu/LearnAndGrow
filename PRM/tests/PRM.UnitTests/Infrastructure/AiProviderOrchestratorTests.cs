using FluentAssertions;
using PRM.Application.Interfaces.Ai;
using PRM.Infrastructure.Ai;

namespace PRM.UnitTests.Infrastructure;

public class AiProviderOrchestratorTests
{
    [Fact]
    public async Task MatchSkillsAsync_DelegatesToConfiguredProvider()
    {
        var request = new SkillMatchAiRequest("need java dev", "Alpha", []);
        var expected = new SkillMatchAiResponse([new RankedCandidateAiResult(1, "Anil", "Good fit", 50)]);

        var provider = new FakeAiProvider(_ => Task.FromResult(expected));
        var orchestrator = new AiProviderOrchestrator(provider);

        var result = await orchestrator.MatchSkillsAsync(request, CancellationToken.None);

        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].Name.Should().Be("Anil");
        provider.CallCount.Should().Be(1);
    }

    private sealed class FakeAiProvider(Func<SkillMatchAiRequest, Task<SkillMatchAiResponse>> match) : IAiProvider
    {
        public string ProviderName => "Gemma";
        public int CallCount { get; private set; }

        public Task<SkillMatchAiResponse> MatchSkillsAsync(
            SkillMatchAiRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return match(request);
        }

        public Task<TeamSkillMatchAiResponse> MatchTeamAsync(
            TeamSkillMatchAiRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TeamSkillMatchAiResponse([], [], []));

        public Task<RiskSummaryAiResponse> SummarizeRiskAsync(
            RiskSummaryAiRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RiskSummaryAiResponse("Risk paragraph"));
    }
}
