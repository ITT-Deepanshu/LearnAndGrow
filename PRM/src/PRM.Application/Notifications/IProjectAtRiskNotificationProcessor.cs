namespace PRM.Application.Notifications;

public interface IProjectAtRiskNotificationProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
