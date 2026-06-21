using FluentAssertions;
using NSubstitute;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Timesheets;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Timesheets;

public class ListTeamTimesheetsServiceTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IActivityTagRepository _tags = Substitute.For<IActivityTagRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ListTeamTimesheetsServiceTests()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);
        _clock.Today.Returns(new DateOnly(2026, 6, 10));
        _projects.ListAsync(5, Arg.Any<CancellationToken>()).Returns([TestFixtures.CreateProject(managerId: 5)]);
    }

    private TimesheetService CreateService() => new(
        _timesheets, _allocations, _tags, _config, _employees, _projects, _current, _uow, _clock);

    [Fact]
    public async Task ListTeamTimesheetsAsync_IncludesMissedRowsWithZeroHours()
    {
        var weekStart = new DateOnly(2026, 6, 1);
        var allocation = TestFixtures.CreateAllocation(4, 201, 50, weekStart, weekStart.AddDays(30), 100);
        TestFixtures.SetProperty(allocation, "ResourceProfile", TestFixtures.CreateResourceProfile(userId: 20, id: 4));
        TestFixtures.SetProperty(allocation, "Project", TestFixtures.CreateProject(id: 201, managerId: 5));

        _allocations.ListActiveOnProjectAsync(201, Arg.Any<CancellationToken>()).Returns([allocation]);
        _timesheets.ListForTeamWeekAsync(Arg.Any<IReadOnlyList<long>>(), weekStart, Arg.Any<CancellationToken>())
            .Returns([Timesheet.CreateMissed(4, weekStart, 0, TestFixtures.FixedUtc)]);

        var rows = await CreateService().ListTeamTimesheetsAsync(weekStart, CancellationToken.None);

        rows.Should().ContainSingle();
        rows[0].Hours.Should().Be(0);
        rows[0].Status.Should().Be("Missed");
    }
}
