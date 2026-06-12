using FluentAssertions;
using NSubstitute;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Users;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Users;

public class UserManagementServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UserManagementServiceTests()
    {
        _current.UserId.Returns(1L);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
    }

    private UserService CreateService() => new(
        _users, _employees, _audit, _hasher, _current, _uow, _clock);

    [Fact]
    public async Task ListUsersAsync_ReturnsMappedUsers()
    {
        var user = TestFixtures.CreateUser(UserRole.Admin, id: 5);
        _users.ListAsync(Arg.Any<CancellationToken>()).Returns([user]);

        var result = await CreateService().ListUsersAsync(CancellationToken.None);

        result.Should().ContainSingle(u => u.Id == 5 && u.Role == "ADMIN");
    }

    [Fact]
    public async Task ResetUserPasswordAsync_UpdatesPasswordAndAudit()
    {
        var user = TestFixtures.CreateUser(UserRole.Resource, id: 8);
        _users.GetByIdAsync(8, Arg.Any<CancellationToken>()).Returns(user);

        await CreateService().ResetUserPasswordAsync(8, new ResetUserPasswordDto("NewPass1", "NewPass1"), CancellationToken.None);

        user.RequiresPasswordChange.Should().BeTrue();
        _audit.Received(1).Add(Arg.Any<AuditLog>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateUserAsync_ThrowsWhenDeactivatingSelf()
    {
        var user = TestFixtures.CreateUser(UserRole.Admin, id: 1);
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateService().DeactivateUserAsync(1, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*your own account*");
    }

    [Fact]
    public async Task DeactivateUserAsync_DeactivatesUserAndProfile()
    {
        var user = TestFixtures.CreateUser(UserRole.Resource, id: 9);
        var profile = TestFixtures.CreateResourceProfile(userId: 9, id: 20);
        _users.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns(user);
        _employees.GetByUserIdAsync(9, Arg.Any<CancellationToken>()).Returns(profile);

        await CreateService().DeactivateUserAsync(9, CancellationToken.None);

        user.IsActive.Should().BeFalse();
        profile.Status.Should().Be(ResourceProfileStatus.Inactive);
    }
}
