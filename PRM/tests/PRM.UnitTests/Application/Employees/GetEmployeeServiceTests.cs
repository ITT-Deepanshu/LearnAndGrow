using FluentAssertions;
using NSubstitute;
using PRM.Application.Employees;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Employees;

public class GetEmployeeServiceTests
{
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private EmployeeService CreateService() => new(
        _employees, _users, _audit, _current, _uow, _clock);

    [Fact]
    public async Task GetEmployeeByIdAsync_AllowsManagerToViewActiveEmployeeForAllocation()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        TestFixtures.SetProperty(resourceProfile, "ManagerId", null);
        _employees.GetByIdWithSkillsAsync(4, Arg.Any<CancellationToken>()).Returns(resourceProfile);

        var result = await CreateService().GetEmployeeByIdAsync(4, CancellationToken.None);

        result.Id.Should().Be(4);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ThrowsWhenManagerViewsInactiveEmployee()
    {
        _current.UserId.Returns(5L);
        _current.Role.Returns(UserRole.Manager);

        var resourceProfile = TestFixtures.CreateResourceProfile(userId: 20, id: 4);
        resourceProfile.Deactivate(1, TestFixtures.FixedUtc);
        TestFixtures.SetProperty(resourceProfile, "ManagerId", null);
        _employees.GetByIdWithSkillsAsync(4, Arg.Any<CancellationToken>()).Returns(resourceProfile);

        var act = () => CreateService().GetEmployeeByIdAsync(4, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
