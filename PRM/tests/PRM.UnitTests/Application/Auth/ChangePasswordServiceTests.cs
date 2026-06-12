using FluentAssertions;
using NSubstitute;
using PRM.Application.Auth;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Auth;

public class ChangePasswordServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ChangePasswordServiceTests()
    {
        _current.UserId.Returns(2L);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Hash(Arg.Any<string>()).Returns("new-hash");
        _tokens.GenerateAccessToken(Arg.Any<PRM.Domain.Entities.User>()).Returns("fresh-token");
    }

    private AuthService CreateService() => new(
        _users, _audit, _hasher, _tokens, _current, _uow, _clock);

    [Fact]
    public async Task ChangePasswordAsync_UpdatesPasswordWhenForcedChange()
    {
        var user = TestFixtures.CreateUser(UserRole.Resource, id: 2);
        _users.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByIdWithDetailsAsync(2, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateService().ChangePasswordAsync(
            new ChangePasswordDto(string.Empty, "NewPass1", "NewPass1"),
            CancellationToken.None);

        result.AccessToken.Should().Be("fresh-token");
        user.RequiresPasswordChange.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_ThrowsWhenCurrentPasswordWrong()
    {
        var user = TestFixtures.CreateUser(UserRole.Manager, id: 2);
        user.ChangePassword("existing-hash", 2, TestFixtures.FixedUtc);
        _users.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "existing-hash").Returns(false);

        var act = () => CreateService().ChangePasswordAsync(
            new ChangePasswordDto("wrong", "NewPass1", "NewPass1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Current password*");
    }
}
