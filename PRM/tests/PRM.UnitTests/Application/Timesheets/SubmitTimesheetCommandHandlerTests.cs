using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Timesheets.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Timesheets;

public class SubmitTimesheetCommandHandlerTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IActivityTagRepository _tags = Substitute.For<IActivityTagRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public SubmitTimesheetCommandHandlerTests()
    {
        _current.UserId.Returns(2L);
        _current.Role.Returns(UserRole.Employee);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);

        _employees.GetByUserIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateEmployee(userId: 2));
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _tags.ListAllAsync(Arg.Any<CancellationToken>()).Returns([ActivityTag.Create(1, "Backend API")]);
        _timesheets.GetByEmployeeWeekAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Timesheet?)null);
        _allocations.ListActiveForEmployeeAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))]);
    }

    private SubmitTimesheetCommandHandler CreateHandler() => new(
        _timesheets, _allocations, _tags, _config, _employees, _current, _uow, _clock);

    [Fact]
    public async Task Handle_ThrowsForFutureWeek()
    {
        var command = new SubmitTimesheetCommand(
            new DateOnly(2026, 5, 18),
            [new SubmitTimesheetEntryCommand(201, 10, [1], null)]);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*future*");
    }

    [Fact]
    public async Task Handle_ThrowsForDuplicateWeek()
    {
        _timesheets.GetByEmployeeWeekAsync(10, new DateOnly(2026, 5, 11), Arg.Any<CancellationToken>())
            .Returns(Timesheet.CreateMissed(10, new DateOnly(2026, 5, 11), 1, TestFixtures.FixedUtc));

        var command = new SubmitTimesheetCommand(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryCommand(201, 10, [1], null)]);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenProjectNotAllocated()
    {
        var command = new SubmitTimesheetCommand(
            new DateOnly(2026, 5, 11),
            [new SubmitTimesheetEntryCommand(999, 10, [1], null)]);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not allocated*");
    }
}
