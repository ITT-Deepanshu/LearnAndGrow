using FluentAssertions;
using NSubstitute;
using PRM.Application.Allocations;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Allocations;

public class ListMyAllocationsServiceTests
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
    public async Task ListMyAllocationsAsync_ReturnsAllocationsForCurrentUserProfile()
    {
        _current.UserId.Returns(20L);
        _current.Role.Returns(UserRole.Resource);
        TestFixtures.SetupPermissions(_current, UserRole.Resource);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        _employees.GetByUserIdAsync(20, Arg.Any<CancellationToken>()).Returns(resourceProfile);
        _allocations.ListAllAsync(4, null, Arg.Any<CancellationToken>()).Returns([
            TestFixtures.CreateAllocation(4, 1, 60, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), 100)
        ]);

        var result = await CreateService().ListMyAllocationsAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ResourceProfileId.Should().Be(4);
    }

    [Fact]
    public async Task ListMyAllocationsAsync_ThrowsWhenNotResourceRole()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);

        var act = () => CreateService().ListMyAllocationsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
