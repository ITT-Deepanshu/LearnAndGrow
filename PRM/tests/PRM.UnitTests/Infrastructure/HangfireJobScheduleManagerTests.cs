using FluentAssertions;
using PRM.Infrastructure.Scheduler;

namespace PRM.UnitTests.Infrastructure;

public class HangfireJobScheduleManagerTests
{
    [Fact]
    public void NoOpJobScheduleManager_DoesNotThrow()
    {
        var manager = new NoOpJobScheduleManager();
        var act = () =>
        {
            manager.ScheduleRecurringJob(30);
            manager.EnqueueImmediateRun();
        };

        act.Should().NotThrow();
    }
}
