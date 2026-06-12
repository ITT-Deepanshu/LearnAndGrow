using Hangfire;
using Hangfire.Common;
using PRM.Application.Interfaces.Scheduling;

namespace PRM.Infrastructure.Scheduler;

public sealed class HangfireJobScheduleManager(
    IRecurringJobManager recurringJobManager,
    IBackgroundJobClient backgroundJobClient) : IPrmJobScheduleManager
{
    public const string ScheduledJobsId = "prm-scheduled-jobs";

    public void ScheduleRecurringJob(int intervalMinutes)
    {
        recurringJobManager.AddOrUpdate(
            ScheduledJobsId,
            Job.FromExpression<IPrmBackgroundJobRunner>(runner => runner.RunScheduledJobsAsync(CancellationToken.None)),
            ToCronExpression(intervalMinutes));
    }

    public void EnqueueImmediateRun() =>
        backgroundJobClient.Enqueue<IPrmBackgroundJobRunner>(runner => runner.RunScheduledJobsAsync(CancellationToken.None));

    internal static string ToCronExpression(int intervalMinutes)
    {
        intervalMinutes = Math.Max(1, intervalMinutes);

        if (intervalMinutes < 60)
            return $"*/{intervalMinutes} * * * *";

        if (intervalMinutes % 60 != 0)
            return "0 * * * *";

        var hours = intervalMinutes / 60;
        return hours < 24
            ? $"0 */{hours} * * *"
            : $"0 0 */{hours / 24} * *";
    }
}
