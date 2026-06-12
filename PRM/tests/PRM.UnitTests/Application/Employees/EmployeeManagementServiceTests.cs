using FluentAssertions;
using NSubstitute;
using PRM.Application.Employees;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Employees;

public class EmployeeManagementServiceTests
{
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public EmployeeManagementServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
    }

    private EmployeeService CreateService() => new(
        _employees, _users, _audit, _current, _uow, _clock);

    [Fact]
    public async Task ListEmployeesAsync_ReturnsEmployeesForAdmin()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 2, id: 10);
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([profile]);

        var result = await CreateService().ListEmployeesAsync(null, null, CancellationToken.None);

        result.Should().ContainSingle(e => e.Id == 10);
    }

    [Fact]
    public async Task ListEmployeesAsync_ThrowsForResourceRole()
    {
        _current.Role.Returns(UserRole.Resource);

        var act = () => CreateService().ListEmployeesAsync(null, null, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateEmployeeAsync_UpdatesDepartmentAndDesignation()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 2, id: 10);
        _employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(profile);

        await CreateService().UpdateEmployeeAsync(10, new UpdateEmployeeDto("Backend", "Senior Dev"), CancellationToken.None);

        profile.Department.Should().Be("Backend");
        profile.Designation.Should().Be("Senior Dev");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_DeactivatesProfileAndUser()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 2, id: 10);
        _employees.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(profile);
        _users.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(profile.User);

        await CreateService().DeactivateEmployeeAsync(10, CancellationToken.None);

        profile.Status.Should().Be(ResourceProfileStatus.Inactive);
        profile.User.IsActive.Should().BeFalse();
    }
}
