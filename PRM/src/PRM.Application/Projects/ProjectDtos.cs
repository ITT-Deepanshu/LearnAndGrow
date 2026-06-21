using PRM.Domain.Enums;

namespace PRM.Application.Projects;

public sealed record CreateProjectDto(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    long ManagerId,
    int TotalStoryPoints);

public sealed record UpdateProjectDto(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    long ManagerId,
    int TotalStoryPoints);

public sealed record ProjectDto(
    long Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    long ManagerId,
    string ManagerName,
    int TotalStoryPoints,
    string Health);

public sealed record ProjectListItemDto(
    long Id,
    string Name,
    string ManagerName,
    DateOnly EndDate,
    string Status,
    string Health);

public sealed record ProjectDetailDto(
    long Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    long ManagerId,
    string ManagerName,
    int TotalStoryPoints,
    string Health,
    string? HealthReason,
    IReadOnlyList<MilestoneDto> Milestones,
    IReadOnlyList<AllocationSummaryDto> Allocations);

public sealed record AddMilestoneDto(string Title, DateOnly DueDate, int StoryPoints);

public sealed record MilestoneDto(
    long Id,
    long ProjectId,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    string Status);

public sealed record UpdateMilestoneStatusDto(MilestoneStatus Status);

public sealed record AllocationSummaryDto(
    long Id,
    long ResourceProfileId,
    string EmployeeName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record ProjectHealthDto(
    long ProjectId,
    string Status,
    string DisplayLabel,
    IReadOnlyList<string> RiskFlags);
