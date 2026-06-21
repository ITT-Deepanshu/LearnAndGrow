namespace PRM.Application.Notifications;

public interface INotificationService
{
    Task ProcessTimesheetComplianceEmailsAsync(CancellationToken cancellationToken = default);

    Task ProcessProjectAtRiskEmailsAsync(CancellationToken cancellationToken = default);
}
