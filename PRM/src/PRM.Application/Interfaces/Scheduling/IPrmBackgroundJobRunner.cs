namespace PRM.Application.Interfaces.Scheduling;

public interface IPrmBackgroundJobRunner
{
    Task RunScheduledJobsAsync(CancellationToken cancellationToken = default);
}
