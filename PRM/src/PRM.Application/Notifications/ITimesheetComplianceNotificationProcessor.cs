namespace PRM.Application.Notifications;

public interface ITimesheetComplianceNotificationProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
