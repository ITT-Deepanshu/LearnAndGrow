namespace PRM.Application.Interfaces.Ai;

public interface IAiProvider
{
    string ProviderName { get; }
    Task<SkillMatchAiResponse> MatchSkillsAsync(SkillMatchAiRequest request, CancellationToken cancellationToken = default);
    Task<RiskSummaryAiResponse> SummarizeRiskAsync(RiskSummaryAiRequest request, CancellationToken cancellationToken = default);
}

public sealed record SkillMatchAiRequest(
    string RequirementText,
    string ProjectName,
    IReadOnlyList<CandidateAiContext> Candidates);

public sealed record CandidateAiContext(
    long EmployeeId,
    string Name,
    string Department,
    IReadOnlyList<string> Skills,
    decimal FreeUtilisation,
    int FreeHoursPerWeek,
    IReadOnlyList<string> RecentActivityTags);

public sealed record SkillMatchAiResponse(IReadOnlyList<RankedCandidateAiResult> Candidates);

public sealed record RankedCandidateAiResult(
    long EmployeeId,
    string Name,
    string Reason,
    decimal? SuggestedUtilisation);

public sealed record RiskSummaryAiRequest(
    string ProjectName,
    IReadOnlyList<MilestoneAiContext> Milestones,
    IReadOnlyList<string> AllocatedPeople,
    IReadOnlyList<EffortAiContext> RecentEffort);

public sealed record MilestoneAiContext(string Title, DateOnly DueDate, string Status);
public sealed record EffortAiContext(string EmployeeName, decimal ExpectedHours, decimal LoggedHours);
public sealed record RiskSummaryAiResponse(string Paragraph);
