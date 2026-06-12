namespace PRM.ConsoleClient.Models;

public sealed record LoginRequest(string Username, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
public sealed record LoginResponse(
    string AccessToken,
    bool RequiresPasswordChange,
    string Role,
    string FullName,
    long? ResourceProfileId);
public sealed record MeResponse(
    long Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    bool RequiresPasswordChange,
    long? ResourceProfileId);

public sealed record CreateUserRequest(string FullName, string Email, string Username, string TemporaryPassword, string Role);
public sealed record UserResponse(long Id, string Username, string Email, string FullName, string Role, bool IsActive, bool RequiresPasswordChange);
public sealed record UserListItem(long Id, string Username, string Role, bool IsActive);
public sealed record ResetPasswordRequest(string NewPassword, string ConfirmPassword);

public sealed record EmployeeListItem(
    long Id,
    long UserId,
    string FullName,
    string Department,
    string Designation,
    string Status,
    long? ManagerId,
    string ManagerName,
    int SkillCount);

public sealed record ResourceProfileSkill(long Id, string Name, string Category, string Proficiency);

public sealed record EmployeeDetail(
    long Id,
    long UserId,
    string Username,
    string Email,
    string FullName,
    string Department,
    string Designation,
    string Status,
    long? ManagerId,
    string ManagerName,
    DateTime? JoinedAt,
    IReadOnlyList<ResourceProfileSkill> Skills);

public sealed record UpdateEmployeeRequest(string Department, string Designation);
public sealed record AssignManagerRequest(long ManagerId);
public sealed record AddSkillRequest(string Name, int Category, int Proficiency);
public sealed record UpdateSkillProficiencyRequest(int Proficiency);

public sealed record CreateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int Status,
    long ManagerId,
    int TotalStoryPoints);

public sealed record UpdateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int Status,
    long ManagerId,
    int TotalStoryPoints);

public sealed record ProjectListItem(long Id, string Name, string ManagerName, DateOnly EndDate, string Status, string Health);

public sealed record ProjectDetail(
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
    IReadOnlyList<Milestone> Milestones,
    IReadOnlyList<AllocationSummary> Allocations);

public sealed record Milestone(long Id, long ProjectId, string Title, DateOnly DueDate, int StoryPoints, string Status);
public sealed record AllocationSummary(long Id, long ResourceProfileId, string EmployeeName, decimal UtilisationPercentage, DateOnly FromDate, DateOnly ToDate);
public sealed record ProjectHealth(long ProjectId, string Status, string DisplayLabel, IReadOnlyList<string> RiskFlags);

public sealed record AddMilestoneRequest(string Title, DateOnly DueDate, int StoryPoints);
public sealed record UpdateMilestoneStatusRequest(int Status);

public sealed record CreateAllocationRequest(
    long ProjectId,
    long ResourceProfileId,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record Allocation(
    long Id,
    long ResourceProfileId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly? EndedAt);

public sealed record AllocationListItem(
    long Id,
    long ResourceProfileId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record SubmitTimesheetRequest(DateOnly WeekStart, IReadOnlyList<SubmitTimesheetEntryRequest> Entries);
public sealed record SubmitTimesheetEntryRequest(long ProjectId, decimal Hours, IReadOnlyList<int> TagIds, string? CustomText);

public sealed record TimesheetEntry(long Id, long ProjectId, string ProjectName, decimal Hours, IReadOnlyList<string> ActivityTags);

public sealed record Timesheet(
    long Id,
    DateOnly WeekStart,
    string Status,
    DateTime? SubmittedAt,
    decimal TotalHours,
    IReadOnlyList<TimesheetEntry> Entries);

public sealed record TimesheetListItem(long Id, DateOnly WeekStart, string Status, decimal TotalHours, DateTime? SubmittedAt);
public sealed record TeamTimesheetRow(string EmployeeName, string ProjectName, decimal Hours, string Status);
public sealed record ActivityTagItem(int Id, string Name, bool IsCustom);
public sealed record TimesheetSubmissionContext(int MaxWeeklyHours, IReadOnlyList<ActivityTagItem> ActivityTags);
public sealed record TimesheetReminder(DateOnly WeekStart, string Message);

public sealed record ResourceDashboard(
    DateOnly AsOfDate,
    DashboardCounts Counts,
    IReadOnlyList<BenchEmployeeRow> OnBench,
    IReadOnlyList<ActiveEmployeeRow> PartiallyAllocated,
    IReadOnlyList<ActiveEmployeeRow> FullyAllocated,
    IReadOnlyList<SkillCategorySummary> SkillsSummary,
    IReadOnlyList<EmployeeDrillDown> DrillDown);

public sealed record DashboardCounts(int BenchCount, int PartiallyAllocatedCount, int FullyAllocatedCount);

public sealed record BenchEmployeeRow(long Id, string FullName, string Department, IReadOnlyList<string> Skills);

public sealed record ActiveEmployeeRow(
    long Id,
    string FullName,
    string Department,
    decimal UtilisationPercentage,
    decimal AvailabilityPercentage,
    IReadOnlyList<string> Skills);

public sealed record SkillCategorySummary(string Category, IReadOnlyList<SkillCount> Skills);
public sealed record SkillCount(string SkillName, int AvailableCount);

public sealed record EmployeeDrillDown(
    long Id,
    string FullName,
    string Department,
    string Designation,
    string Status,
    decimal UtilisationPercentage,
    decimal AvailabilityPercentage,
    IReadOnlyList<string> ProfileSkills,
    IReadOnlyList<EmployeeAllocationRow> ActiveAllocations,
    IReadOnlyList<string> RecentActivityTags);

public sealed record EmployeeAllocationRow(string ProjectName, decimal UtilisationPercentage, DateOnly FromDate, DateOnly ToDate);

public sealed record SkillMatchResult(IReadOnlyList<RankedCandidate> Candidates, string? Note);
public sealed record RankedCandidate(long ResourceProfileId, string Name, string Reason, decimal? SuggestedUtilisation);

public sealed record TeamSkillMatchRequest(string Requirement);
public sealed record TeamSkillMatchResult(
    string Requirement,
    IReadOnlyList<TeamRoleDefinition> TeamDefined,
    IReadOnlyList<TeamAssignment> Assignments,
    IReadOnlyList<UnfilledRole> Unfilled,
    string? Note);
public sealed record TeamRoleDefinition(string RoleTitle, int Count, IReadOnlyList<RequiredSkill> RequiredSkills);
public sealed record RequiredSkill(string Name, string MinProficiency);
public sealed record TeamAssignment(
    string RoleTitle,
    int SlotNumber,
    long ResourceProfileId,
    string EmployeeName,
    string ManagerName,
    IReadOnlyList<string> Skills,
    string Why);
public sealed record UnfilledRole(string RoleTitle, int UnfilledCount, string Reason, string Detail);

public sealed record RiskSummary(string Paragraph, RiskSummarySnapshot Snapshot);
public sealed record RiskSummarySnapshot(IReadOnlyList<string> OverdueMilestones, IReadOnlyList<string> LowEffortPeople, string HealthLabel);

public sealed record UpdateSystemConfigRequest(int LlmProvider, string? LlmApiKey, int SchedulerIntervalMinutes, int MaxWeeklyHours);
public sealed record SystemConfig(string LlmProvider, string LlmApiKeyMasked, int SchedulerIntervalMinutes, int MaxWeeklyHours);

public sealed record StoredTokens(string? AccessToken, long? ResourceProfileId);

public sealed record ProblemDetailsResponse(string? Title, string? Detail, int? Status);
