using FluentAssertions;
using NSubstitute;
using PRM.Application.Allocations;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Allocations;

public class CreateAllocationServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateAllocationServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);

        var project = TestFixtures.CreateProject(managerId: 1);
        _projects.GetByIdAsync(201, Arg.Any<CancellationToken>()).Returns(project);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 2);
        _employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(resourceProfile);
    }

    private AllocationService CreateService() => new(
        _projects, _employees, _allocations, _audit, _current, _uow, _clock);

    [Fact]
    public async Task CreateAllocationAsync_ThrowsWhenUtilisationWouldExceed100()
    {
        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateAllocation(10, 202, 80, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31))]);

        var dto = new CreateAllocationDto(201, 10, 30, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));
        var act = () => CreateService().CreateAllocationAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*exceed 100%*");
    }

    [Fact]
    public async Task CreateAllocationAsync_ThrowsWhenManagerDoesNotOwnProject()
    {
        _current.UserId.Returns(99L);
        var dto = new CreateAllocationDto(201, 10, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));

        var act = () => CreateService().CreateAllocationAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateAllocationAsync_ThrowsWhenTargetIsNotResourceRole()
    {
        var managerProfile = TestFixtures.CreateResourceProfile(userId: 3, id: 11);
        TestFixtures.SetProperty(managerProfile, "User", TestFixtures.CreateUser(UserRole.Manager, id: 3));
        _employees.GetByIdAsync(11, Arg.Any<CancellationToken>()).Returns(managerProfile);

        var dto = new CreateAllocationDto(201, 11, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));
        var act = () => CreateService().CreateAllocationAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Resource role*");
    }

    [Fact]
    public async Task CreateAllocationAsync_CreatesAllocationWhenValid()
    {
        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _allocations.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var allocation = TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30), 500);
                TestFixtures.SetProperty(allocation, "ResourceProfile", TestFixtures.CreateResourceProfile());
                TestFixtures.SetProperty(allocation, "Project", TestFixtures.CreateProject());
                return allocation;
            });

        var dto = new CreateAllocationDto(201, 10, 50, new DateOnly(2026, 5, 14), new DateOnly(2026, 6, 30));
        var result = await CreateService().CreateAllocationAsync(dto, CancellationToken.None);

        result.UtilisationPercentage.Should().Be(50);
        _allocations.Received(1).Add(Arg.Any<Allocation>());
    }
}
