using FluentAssertions;
using NSubstitute;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Timesheets;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Timesheets;

public class TimesheetComplianceServiceTests
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

    public TimesheetComplianceServiceTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _tags.ListAllAsync(Arg.Any<CancellationToken>()).Returns([ActivityTag.Create(1, "Backend API")]);
        _allocations.ListActiveForEmployeeAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(4, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
    }

    private TimesheetService CreateService() => new(
        _timesheets, _allocations, _tags, _config, _employees, _projects, _current, _uow, _clock);

    [Fact]
    public async Task SubmitTimesheetAsync_ThrowsWhenSubmissionIsFrozen()
    {
        _current.UserId.Returns(20L);
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);

        var profile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        profile.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        _employees.GetByUserIdAsync(20, Arg.Any<CancellationToken>()).Returns(profile);

        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryDto(201, 10, [1], null)]);

        var act = () => CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*restricted*");
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ClearsComplianceStateAfterSuccessfulSubmit()
    {
        _current.UserId.Returns(20L);
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);

        var profile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 11), 0, TestFixtures.FixedUtc);
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);
        _employees.GetByUserIdAsync(20, Arg.Any<CancellationToken>()).Returns(profile);

        var submitted = Timesheet.Submit(
            4,
            new DateOnly(2026, 5, 11),
            [TimesheetEntry.Create(201, 10, [ActivityTag.Create(1, "Backend API")], 20, 1, TestFixtures.FixedUtc)],
            40,
            1,
            TestFixtures.FixedUtc);
        _timesheets.GetByEmployeeWeekAsync(4, new DateOnly(2026, 5, 11), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null, submitted);

        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryDto(201, 10, [1], null)]);

        await CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        profile.TimesheetSubmissionFrozen.Should().BeFalse();
        profile.TimesheetReminderCount.Should().Be(0);
        profile.MissedTimesheetWeekStart.Should().BeNull();
    }

    [Fact]
    public async Task RestoreTimesheetSubmissionAsync_RestoresDirectReport()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);

        var employee = TestFixtures.CreateResourceProfileWithManager(managerUserId: 5);
        employee.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(employee);

        await CreateService().RestoreTimesheetSubmissionAsync(4, CancellationToken.None);

        employee.TimesheetSubmissionFrozen.Should().BeFalse();
        employee.TimesheetReminderCount.Should().Be(0);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreTimesheetSubmissionAsync_ThrowsWhenEmployeeIsNotDirectReport()
    {
        _current.UserId.Returns(99L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);

        var employee = TestFixtures.CreateResourceProfileWithManager(managerUserId: 5);
        employee.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(employee);

        var act = () => CreateService().RestoreTimesheetSubmissionAsync(4, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RestoreTimesheetSubmissionAsync_ThrowsWhenNotFrozen()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);

        var employee = TestFixtures.CreateResourceProfileWithManager(managerUserId: 5);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(employee);

        var act = () => CreateService().RestoreTimesheetSubmissionAsync(4, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not restricted*");
    }

    [Fact]
    public async Task ListTeamTimesheetsAsync_ExposesSubmissionFrozenFlag()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);

        var weekStart = new DateOnly(2026, 6, 1);
        _projects.ListAsync(5, Arg.Any<CancellationToken>()).Returns([TestFixtures.CreateProject(managerId: 5, id: 201)]);

        var profile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        profile.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        var allocation = TestFixtures.CreateAllocation(4, 201, 50, weekStart, weekStart.AddDays(30), 100);
        TestFixtures.SetProperty(allocation, "ResourceProfile", profile);
        TestFixtures.SetProperty(allocation, "Project", TestFixtures.CreateProject(id: 201, managerId: 5));

        _allocations.ListActiveOnProjectAsync(201, Arg.Any<CancellationToken>()).Returns([allocation]);
        _timesheets.ListForTeamWeekAsync(Arg.Any<IReadOnlyList<long>>(), weekStart, Arg.Any<CancellationToken>())
            .Returns([Timesheet.CreateMissed(4, weekStart, 0, TestFixtures.FixedUtc)]);

        var rows = await CreateService().ListTeamTimesheetsAsync(weekStart, CancellationToken.None);

        rows.Should().ContainSingle();
        rows[0].SubmissionFrozen.Should().BeTrue();
        rows[0].ResourceProfileId.Should().Be(4);
    }
}
