using FluentAssertions;
using NSubstitute;
using PRM.Application.Dashboard;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Dashboard;

public class GetResourceDashboardServiceTests
{
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetResourceDashboardServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);
        _clock.Today.Returns(TestFixtures.FixedToday);

        var bench = TestFixtures.CreateResourceProfile(userId: 2, id: 10, status: ResourceProfileStatus.Bench);
        var partial = TestFixtures.CreateResourceProfile(userId: 3, id: 11, status: ResourceProfileStatus.PartiallyAllocated);
        _employees.ListAsync(null, null, 1L, Arg.Any<CancellationToken>())
            .Returns([bench, partial]);

        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _allocations.ListActiveForEmployeeAsync(11, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(11, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);
    }

    private DashboardService CreateService() => new(
        _employees, _allocations, _timesheets, _current, _clock);

    [Fact]
    public async Task GetResourceDashboardAsync_ReturnsDashboardBucketsForManager()
    {
        var result = await CreateService().GetResourceDashboardAsync(CancellationToken.None);

        result.Counts.BenchCount.Should().Be(1);
        result.Counts.PartiallyAllocatedCount.Should().Be(1);
        result.Counts.FullyAllocatedCount.Should().Be(0);
        result.DrillDown.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResourceDashboardAsync_ThrowsWhenNotManager()
    {
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);

        var act = () => CreateService().GetResourceDashboardAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
