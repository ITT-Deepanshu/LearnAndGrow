namespace PRM.Application.Ai;

public sealed record SkillMatchResultDto(
    IReadOnlyList<RankedCandidateDto> Candidates,
    string? Note);

public sealed record RankedCandidateDto(
    long ResourceProfileId,
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

public sealed record TeamSkillMatchRequestDto(string Requirement);

public sealed record TeamSkillMatchResultDto(
    string Requirement,
    IReadOnlyList<TeamRoleDefinitionDto> TeamDefined,
    IReadOnlyList<TeamAssignmentDto> Assignments,
    IReadOnlyList<UnfilledRoleDto> Unfilled,
    string? Note);

public sealed record TeamRoleDefinitionDto(
    string RoleTitle,
    int Count,
    IReadOnlyList<RequiredSkillDto> RequiredSkills);

public sealed record RequiredSkillDto(string Name, string MinProficiency);

public sealed record TeamAssignmentDto(
    string RoleTitle,
    int SlotNumber,
    long ResourceProfileId,
    string EmployeeName,
    string ManagerName,
    IReadOnlyList<string> Skills,
    string Why);

public sealed record UnfilledRoleDto(
    string RoleTitle,
    int UnfilledCount,
    string Reason,
    string Detail);
