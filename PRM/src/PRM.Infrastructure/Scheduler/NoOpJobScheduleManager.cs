using PRM.Application.Interfaces.Scheduling;

namespace PRM.Infrastructure.Scheduler;

public sealed class NoOpJobScheduleManager : IPrmJobScheduleManager
{
    public void ScheduleRecurringJob(int intervalMinutes) { }

    public void EnqueueImmediateRun() { }
}
