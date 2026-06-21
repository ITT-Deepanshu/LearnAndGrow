using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Notifications;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;
using PRM.Infrastructure.Scheduler;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Infrastructure;

public class PrmBackgroundJobRunnerTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();

    public PrmBackgroundJobRunnerTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([]);
        _projects.ListActiveWithDetailsAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    private PrmBackgroundJobRunner CreateRunner() => new(
        _clock,
        _uow,
        _employees,
        _allocations,
        _projects,
        _timesheets,
        _config,
        _notifications,
        NullLogger<PrmBackgroundJobRunner>.Instance);

    [Fact]
    public async Task RunScheduledJobsAsync_CreatesMissedTimesheetWhenAllocationsExist()
    {
        var employee = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([employee]);
        _allocations.ListActiveForEmployeeAsync(4, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(4, 1, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31))]);
        _timesheets.GetByEmployeeWeekAsync(4, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);

        await CreateRunner().RunScheduledJobsAsync(CancellationToken.None);

        _timesheets.Received(1).Add(Arg.Is<Timesheet>(t => t.Status == TimesheetStatus.Missed));
        employee.MissedTimesheetWeekStart.Should().Be(WeekHelper.GetLastCompletedWeekMonday(TestFixtures.FixedToday));
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        _uow.Received(1).ClearChangeTracker();
    }

    [Fact]
    public async Task RunScheduledJobsAsync_InvokesNotificationProcessing()
    {
        await CreateRunner().RunScheduledJobsAsync(CancellationToken.None);

        await _notifications.Received(1).ProcessTimesheetComplianceEmailsAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).ProcessProjectAtRiskEmailsAsync(Arg.Any<CancellationToken>());
        _uow.Received(1).ClearChangeTracker();
    }

    [Fact]
    public async Task RunScheduledJobsAsync_SkipsMissedCreationWhenTimesheetAlreadyExists()
    {
        var employee = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([employee]);
        _allocations.ListActiveForEmployeeAsync(4, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(4, 1, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31))]);
        _timesheets.GetByEmployeeWeekAsync(4, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Timesheet.CreateMissed(4, new DateOnly(2026, 5, 5), 0, TestFixtures.FixedUtc));

        await CreateRunner().RunScheduledJobsAsync(CancellationToken.None);

        _timesheets.DidNotReceive().Add(Arg.Any<Timesheet>());
    }
}
