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

public class EndAllocationServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public EndAllocationServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(TestFixtures.FixedToday);
    }

    private AllocationService CreateService() => new(
        _projects, _employees, _allocations, _audit, _current, _uow, _clock);

    [Fact]
    public async Task EndAllocationAsync_EndsAllocationAndRecomputesStatus()
    {
        var project = TestFixtures.CreateProject(managerId: 1, id: 201);
        var profile = TestFixtures.CreateResourceProfile(userId: 2, id: 10, status: ResourceProfileStatus.Allocated);
        var allocation = TestFixtures.CreateAllocation(10, 201, 100, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31), 500);
        TestFixtures.SetProperty(allocation, "Project", project);
        TestFixtures.SetProperty(allocation, "ResourceProfile", profile);

        _allocations.GetByIdAsync(500, Arg.Any<CancellationToken>()).Returns(allocation);
        _employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(profile);
        _allocations.ListActiveForEmployeeAsync(10, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateService().EndAllocationAsync(500, CancellationToken.None);

        allocation.EndedAt.Should().Be(TestFixtures.FixedToday);
        profile.Status.Should().Be(ResourceProfileStatus.Bench);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAllocationsAsync_RequiresAdminRole()
    {
        var act = () => CreateService().ListAllocationsAsync(null, null, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ListAllocationsAsync_ReturnsAllocationsForAdmin()
    {
        _current.Role.Returns(UserRole.Admin);
        var allocation = TestFixtures.CreateAllocation(10, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30), 501);
        TestFixtures.SetProperty(allocation, "Project", TestFixtures.CreateProject(id: 201));
        TestFixtures.SetProperty(allocation, "ResourceProfile", TestFixtures.CreateResourceProfile(id: 10));
        _allocations.ListAllAsync(null, null, Arg.Any<CancellationToken>()).Returns([allocation]);

        var result = await CreateService().ListAllocationsAsync(null, null, CancellationToken.None);

        result.Should().ContainSingle(a => a.Id == 501);
    }
}
