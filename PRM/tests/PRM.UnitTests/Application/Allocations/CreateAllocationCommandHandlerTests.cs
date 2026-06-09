using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Allocations.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Allocations;

public class CreateAllocationCommandHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateAllocationCommandHandlerTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);

        var project = TestFixtures.CreateProject(managerId: 1);
        _projects.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(project);

        var employee = TestFixtures.CreateEmployee(userId: 2);
        _employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);
    }

    private CreateAllocationCommandHandler CreateHandler() => new(
        _projects, _employees, _allocations, _audit, _current, _uow, _clock);

    [Fact]
    public async Task Handle_ThrowsWhenUtilisationWouldExceed100()
    {
        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(10, 202, 80, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31))]);

        var command = new CreateAllocationCommand(201, 10, 30, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));
        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*exceed 100%*");
    }

    [Fact]
    public async Task Handle_ThrowsWhenManagerDoesNotOwnProject()
    {
        _current.UserId.Returns(99L);
        var command = new CreateAllocationCommand(201, 10, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_CreatesAllocationWhenValid()
    {
        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _allocations.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var allocation = TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30), 500);
                TestFixtures.SetProperty(allocation, "Employee", TestFixtures.CreateEmployee());
                TestFixtures.SetProperty(allocation, "Project", TestFixtures.CreateProject());
                return allocation;
            });

        var command = new CreateAllocationCommand(201, 10, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.UtilisationPercentage.Should().Be(50);
        _allocations.Received(1).Add(Arg.Any<Allocation>());
    }
}
