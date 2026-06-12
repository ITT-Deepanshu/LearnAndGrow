namespace PRM.Application.Interfaces.Ai;

public interface IAiProvider
{
    Task<SkillMatchAiResponse> MatchSkillsAsync(SkillMatchAiRequest request, CancellationToken cancellationToken = default);
    Task<TeamSkillMatchAiResponse> MatchTeamAsync(TeamSkillMatchAiRequest request, CancellationToken cancellationToken = default);
    Task<RiskSummaryAiResponse> SummarizeRiskAsync(RiskSummaryAiRequest request, CancellationToken cancellationToken = default);
}

public sealed record SkillMatchAiRequest(
    string RequirementText,
    string ProjectName,
    IReadOnlyList<CandidateAiContext> Candidates);

public sealed record CandidateAiContext(
    long ResourceProfileId,
    string Name,
    string Department,
    IReadOnlyList<string> Skills,
    decimal FreeUtilisation,
    int FreeHoursPerWeek,
    IReadOnlyList<string> RecentActivityTags);

public sealed record SkillMatchAiResponse(IReadOnlyList<RankedCandidateAiResult> Candidates);

public sealed record RankedCandidateAiResult(
    long ResourceProfileId,
    string Name,
    string Reason,
    decimal? SuggestedUtilisation);

public sealed record TeamSkillMatchAiRequest(
    string RequirementText,
    IReadOnlyList<BenchEmployeeAiContext> BenchEmployees);

public sealed record BenchEmployeeAiContext(
    long ResourceProfileId,
    string Name,
    string ManagerName,
    IReadOnlyList<string> Skills);

public sealed record TeamSkillMatchAiResponse(
    IReadOnlyList<TeamRoleDefinitionAiResult> TeamDefined,
    IReadOnlyList<TeamAssignmentAiResult> Assignments,
    IReadOnlyList<UnfilledRoleAiResult> Unfilled);

public sealed record TeamRoleDefinitionAiResult(
    string RoleTitle,
    int Count,
    IReadOnlyList<RequiredSkillAiResult> RequiredSkills);

public sealed record RequiredSkillAiResult(string Name, string MinProficiency);

public sealed record TeamAssignmentAiResult(
    string RoleTitle,
    int SlotNumber,
    long ResourceProfileId,
    string Why);

public sealed record UnfilledRoleAiResult(
    string RoleTitle,
    int UnfilledCount,
    string Reason,
    string Detail);

public sealed record RiskSummaryAiRequest(
    string ProjectName,
    IReadOnlyList<MilestoneAiContext> Milestones,
    IReadOnlyList<string> AllocatedPeople,
    IReadOnlyList<EffortAiContext> RecentEffort);

public sealed record MilestoneAiContext(string Title, DateOnly DueDate, string Status);
public sealed record EffortAiContext(string EmployeeName, decimal ExpectedHours, decimal LoggedHours);
public sealed record RiskSummaryAiResponse(string Paragraph);
