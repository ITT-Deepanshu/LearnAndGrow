namespace PRM.Application.Notifications.Templates;

public sealed record TimesheetReminderEmailModel(
    string EmployeeName,
    DateOnly WeekStart,
    int ReminderNumber);

public sealed record TimesheetFrozenEmployeeEmailModel(
    string EmployeeName,
    DateOnly WeekStart);

public sealed record TimesheetFrozenManagerEmailModel(
    string EmployeeName,
    string ManagerName,
    DateOnly WeekStart);

public sealed record ProjectAtRiskEmailModel(
    string ProjectName,
    string ManagerName,
    string HealthLabel,
    IReadOnlyList<ProjectMilestoneEmailModel> Milestones,
    IReadOnlyList<string> RiskFlags,
    string AiRiskSummary,
    IReadOnlyList<string> SuggestedBenchResources);

public sealed record ProjectMilestoneEmailModel(
    string Title,
    DateOnly DueDate,
    string Status);
