namespace PRM.Application.Interfaces.Scheduling;

public interface IPrmJobScheduleManager
{
    void ScheduleRecurringJob(int intervalMinutes);

    void EnqueueImmediateRun();
}
