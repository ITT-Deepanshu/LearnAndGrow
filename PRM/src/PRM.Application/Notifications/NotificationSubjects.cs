namespace PRM.Application.Notifications;

internal static class NotificationSubjects
{
    public static string TimesheetReminder(int reminderNumber, DateOnly weekStart) =>
        $"Timesheet reminder {reminderNumber} — week of {weekStart:dd MMM yyyy}";

    public const string TimesheetFrozenEmployee = "Timesheet submission restricted";

    public static string TimesheetFrozenManager(string employeeName) =>
        $"Action required: {employeeName} timesheet frozen";

    public static string ProjectAtRisk(string projectName) =>
        $"Project at risk: {projectName}";
}
