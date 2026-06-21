namespace PRM.Application.Notifications;

public sealed class NotificationService(
    ITimesheetComplianceNotificationProcessor timesheetComplianceProcessor,
    IProjectAtRiskNotificationProcessor projectAtRiskProcessor) : INotificationService
{
    public Task ProcessTimesheetComplianceEmailsAsync(CancellationToken cancellationToken = default) =>
        timesheetComplianceProcessor.ProcessAsync(cancellationToken);

    public Task ProcessProjectAtRiskEmailsAsync(CancellationToken cancellationToken = default) =>
        projectAtRiskProcessor.ProcessAsync(cancellationToken);
}
