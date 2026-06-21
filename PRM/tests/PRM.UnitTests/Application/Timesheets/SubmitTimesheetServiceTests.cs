using FluentAssertions;
using NSubstitute;
using PRM.Application.Timesheets;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Timesheets;

public class SubmitTimesheetServiceTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IActivityTagRepository _tags = Substitute.For<IActivityTagRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public SubmitTimesheetServiceTests()
    {
        _current.UserId.Returns(2L);
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);

        _employees.GetByUserIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateResourceProfile(userId: 2));
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _tags.ListAllAsync(Arg.Any<CancellationToken>()).Returns([ActivityTag.Create(1, "Backend API")]);
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);
        _allocations.ListActiveForEmployeeAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
    }

    private TimesheetService CreateService() => new(
        _timesheets, _allocations, _tags, _config, _employees, Substitute.For<IProjectRepository>(), _current, _uow, _clock);

    [Fact]
    public async Task SubmitTimesheetAsync_ThrowsForFutureWeek()
    {
        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 18),
            [new SubmitTimesheetEntryDto(201, 10, [1], null)]);

        var act = () => CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*future*");
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ThrowsForDuplicateSubmittedWeek()
    {
        var submitted = Timesheet.Submit(
            10,
            new DateOnly(2026, 5, 11),
            [TimesheetEntry.Create(201, 10, [ActivityTag.Create(1, "Backend API")], 20, 1, TestFixtures.FixedUtc)],
            40,
            1,
            TestFixtures.FixedUtc);
        _timesheets.GetByEmployeeWeekAsync(10, new DateOnly(2026, 5, 11), Arg.Any<CancellationToken>())
            .Returns(submitted);

        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryDto(201, 10, [1], null)]);

        var act = () => CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ReplacesMissedTimesheet()
    {
        var missed = Timesheet.CreateMissed(10, new DateOnly(2026, 5, 11), 1, TestFixtures.FixedUtc);
        var submitted = Timesheet.Submit(
            10,
            new DateOnly(2026, 5, 11),
            [TimesheetEntry.Create(201, 10, [ActivityTag.Create(1, "Backend API")], 20, 1, TestFixtures.FixedUtc)],
            40,
            1,
            TestFixtures.FixedUtc);
        _timesheets.GetByEmployeeWeekAsync(10, new DateOnly(2026, 5, 11), Arg.Any<CancellationToken>())
            .Returns(missed, submitted);

        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryDto(201, 10, [1], null)]);

        var result = await CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        result.Status.Should().Be("Submitted");
        _timesheets.Received(1).Remove(Arg.Any<Timesheet>());
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ThrowsWhenProjectNotAllocated()
    {
        var dto = new SubmitTimesheetDto(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryDto(999, 10, [1], null)]);

        var act = () => CreateService().SubmitTimesheetAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not allocated*");
    }
}
