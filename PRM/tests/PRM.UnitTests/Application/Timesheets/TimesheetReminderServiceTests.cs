using FluentAssertions;
using NSubstitute;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Timesheets;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Timesheets;

public class TimesheetReminderServiceTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IActivityTagRepository _tags = Substitute.For<IActivityTagRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateOnly PreviousWeekMonday = new(2026, 5, 4);

    public TimesheetReminderServiceTests()
    {
        _current.UserId.Returns(2L);
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);

        _employees.GetByUserIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateResourceProfile(userId: 2));
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), PreviousWeekMonday, Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);
        _allocations.ListActiveForEmployeeAsync(
                Arg.Any<long>(),
                PreviousWeekMonday,
                PreviousWeekMonday.AddDays(6),
                Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
    }

    private TimesheetService CreateService() => new(
        _timesheets, _allocations, _tags, _config, _employees, Substitute.For<IProjectRepository>(), _current, _uow, _clock);

    [Fact]
    public async Task GetMissedTimesheetReminderAsync_ReturnsReminderWhenAllocatedAndNotSubmitted()
    {
        var result = await CreateService().GetMissedTimesheetReminderAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result!.WeekStart.Should().Be(PreviousWeekMonday);
        result.Message.Should().Contain("04-05-2026");
    }

    [Fact]
    public async Task GetMissedTimesheetReminderAsync_ReturnsNullWhenSubmitted()
    {
        var submitted = Timesheet.Submit(
            10,
            PreviousWeekMonday,
            [TimesheetEntry.Create(201, 10, [ActivityTag.Create(1, "Backend API")], 20, 1, TestFixtures.FixedUtc)],
            40,
            1,
            TestFixtures.FixedUtc);

        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), PreviousWeekMonday, Arg.Any<CancellationToken>())
            .Returns(submitted);

        var result = await CreateService().GetMissedTimesheetReminderAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMissedTimesheetReminderAsync_ReturnsReminderWhenMissedExists()
    {
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), PreviousWeekMonday, Arg.Any<CancellationToken>())
            .Returns(Timesheet.CreateMissed(10, PreviousWeekMonday, 1, TestFixtures.FixedUtc));

        var result = await CreateService().GetMissedTimesheetReminderAsync(CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMissedTimesheetReminderAsync_ReturnsNullWhenNoAllocationsForWeek()
    {
        _allocations.ListActiveForEmployeeAsync(
                Arg.Any<long>(),
                PreviousWeekMonday,
                PreviousWeekMonday.AddDays(6),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateService().GetMissedTimesheetReminderAsync(CancellationToken.None);

        result.Should().BeNull();
    }
}
