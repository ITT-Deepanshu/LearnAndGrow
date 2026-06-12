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

public class ListAllocationsByEmployeeServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private AllocationService CreateService() => new(
        _projects, _employees, _allocations, _audit, _current, _uow, _clock);

    [Fact]
    public async Task ListAllocationsByEmployeeAsync_AllowsManagerForActiveEmployeeOnAnotherTeam()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        TestFixtures.SetProperty(resourceProfile, "ManagerId", 99L);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(resourceProfile);
        _allocations.ListAllAsync(4, null, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateService().ListAllocationsByEmployeeAsync(4, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAllocationsByEmployeeAsync_ThrowsWhenManagerViewsInactiveEmployee()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        resourceProfile.Deactivate(1, TestFixtures.FixedUtc);
        TestFixtures.SetProperty(resourceProfile, "ManagerId", 99L);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(resourceProfile);

        var act = () => CreateService().ListAllocationsByEmployeeAsync(4, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ListAllocationsByEmployeeAsync_AllowsManagerForDirectReport()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        TestFixtures.SetProperty(resourceProfile, "ManagerId", 5L);
        _employees.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(resourceProfile);
        _allocations.ListAllAsync(4, null, Arg.Any<CancellationToken>()).Returns([
            TestFixtures.CreateAllocation(4, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31), 900)
        ]);

        var result = await CreateService().ListAllocationsByEmployeeAsync(4, CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
