using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Dashboard.Queries;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Dashboard;

public class GetResourceDashboardQueryHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetResourceDashboardQueryHandlerTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        _clock.Today.Returns(TestFixtures.FixedToday);

        var bench = TestFixtures.CreateEmployee(userId: 2, id: 10, status: EmployeeStatus.Bench);
        var partial = TestFixtures.CreateEmployee(userId: 3, id: 11, status: EmployeeStatus.PartiallyAllocated);
        _employees.ListAsync(null, null, 1L, Arg.Any<CancellationToken>())
            .Returns([bench, partial]);

        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _allocations.ListActiveForEmployeeAsync(11, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(11, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);
    }

    private GetResourceDashboardQueryHandler CreateHandler() => new(
        _employees, _allocations, _timesheets, _current, _clock);

    [Fact]
    public async Task Handle_ReturnsDashboardBucketsForManager()
    {
        var result = await CreateHandler().Handle(new GetResourceDashboardQuery(), CancellationToken.None);

        result.Counts.BenchCount.Should().Be(1);
        result.Counts.PartiallyAllocatedCount.Should().Be(1);
        result.Counts.FullyAllocatedCount.Should().Be(0);
        result.DrillDown.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ThrowsWhenNotManager()
    {
        _current.Role.Returns(UserRole.Employee);

        var act = () => CreateHandler().Handle(new GetResourceDashboardQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
