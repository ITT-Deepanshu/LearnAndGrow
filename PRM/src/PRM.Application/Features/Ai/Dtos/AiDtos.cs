namespace PRM.Application.Features.Ai.Dtos;

public sealed record SkillMatchResultDto(
    IReadOnlyList<RankedCandidateDto> Candidates,
    string? Note);

public sealed record RankedCandidateDto(
    long EmployeeId,
    string Name,
    string Reason,
    decimal? SuggestedUtilisation);

public sealed record RiskSummaryDto(
    string Paragraph,
    RiskSummarySnapshotDto Snapshot);

public sealed record RiskSummarySnapshotDto(
    IReadOnlyList<string> OverdueMilestones,
    IReadOnlyList<string> LowEffortPeople,
    string HealthLabel);
