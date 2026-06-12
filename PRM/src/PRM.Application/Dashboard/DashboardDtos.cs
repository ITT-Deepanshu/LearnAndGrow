namespace PRM.Application.Dashboard;

public sealed record ResourceDashboardDto(
    DateOnly AsOfDate,
    DashboardCountsDto Counts,
    IReadOnlyList<BenchEmployeeRowDto> OnBench,
    IReadOnlyList<ActiveEmployeeRowDto> PartiallyAllocated,
    IReadOnlyList<ActiveEmployeeRowDto> FullyAllocated,
    IReadOnlyList<SkillCategorySummaryDto> SkillsSummary,
    IReadOnlyList<EmployeeDrillDownDto> DrillDown);

public sealed record DashboardCountsDto(
    int BenchCount,
    int PartiallyAllocatedCount,
    int FullyAllocatedCount);

public sealed record BenchEmployeeRowDto(
    long Id,
    string FullName,
    string Department,
    IReadOnlyList<string> Skills);

public sealed record ActiveEmployeeRowDto(
    long Id,
    string FullName,
    string Department,
    decimal UtilisationPercentage,
    decimal AvailabilityPercentage,
    IReadOnlyList<string> Skills);

public sealed record SkillCategorySummaryDto(
    string Category,
    IReadOnlyList<SkillCountDto> Skills);

public sealed record SkillCountDto(
    string SkillName,
    int AvailableCount);

public sealed record EmployeeDrillDownDto(
    long Id,
    string FullName,
    string Department,
    string Designation,
    string Status,
    decimal UtilisationPercentage,
    decimal AvailabilityPercentage,
    IReadOnlyList<string> ProfileSkills,
    IReadOnlyList<EmployeeAllocationRowDto> ActiveAllocations,
    IReadOnlyList<string> RecentActivityTags);

public sealed record EmployeeAllocationRowDto(
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);
