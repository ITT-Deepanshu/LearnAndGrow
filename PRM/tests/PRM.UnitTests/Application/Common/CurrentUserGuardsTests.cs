using FluentAssertions;
using NSubstitute;
using PRM.Application.Common;
using PRM.Application.Interfaces.Common;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.UnitTests.Application.Common;

public class CurrentUserGuardsTests
{
    [Fact]
    public void EnsureAdmin_AllowsAdminRole()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(1L);
        user.Role.Returns(UserRole.Admin);

        var act = () => CurrentUserGuards.EnsureAdmin(user);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureManager_ThrowsForResourceRole()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(2L);
        user.Role.Returns(UserRole.Resource);

        var act = () => CurrentUserGuards.EnsureManager(user);
        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnsProject_ThrowsWhenManagerDoesNotOwnProject()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(5L);
        user.Role.Returns(UserRole.Manager);

        var act = () => CurrentUserGuards.EnsureOwnsProject(user, 99L);
        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void ManagerFilter_ReturnsManagerIdForManager()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(7L);
        user.Role.Returns(UserRole.Manager);

        CurrentUserGuards.ManagerFilter(user).Should().Be(7L);
    }
}
