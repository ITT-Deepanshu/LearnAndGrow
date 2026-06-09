using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Auth.Commands;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public LoginCommandHandlerTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokens.GenerateAccessToken(Arg.Any<User>()).Returns("access");
        _tokens.GenerateRefreshToken().Returns("refresh-plain");
        _tokens.HashToken("refresh-plain").Returns("refresh-hash");
    }

    private LoginCommandHandler CreateHandler() => new(
        _users, _refreshTokens, _audit, _hasher, _tokens, _uow, _clock);

    [Fact]
    public async Task Handle_ReturnsTokensOnValidLogin()
    {
        var user = UserFactory.CreateAccount("admin", "admin@prm.local", "Admin", "hash", UserRole.Admin, 0, TestFixtures.FixedUtc);
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new LoginCommand("admin", "Admin@1234"), CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh-plain");
        result.Role.Should().Be("ADMIN");
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenUserNotFound()
    {
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => CreateHandler().Handle(new LoginCommand("unknown", "x"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenAccountDisabled()
    {
        var user = UserFactory.CreateAccount("inactive", "i@p.local", "Inactive", "hash", UserRole.Employee, 0, TestFixtures.FixedUtc);
        user.Deactivate(0, TestFixtures.FixedUtc);
        _users.GetByUsernameAsync("inactive", Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateHandler().Handle(new LoginCommand("inactive", "x"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenPasswordInvalid()
    {
        var user = UserFactory.CreateAccount("admin", "admin@prm.local", "Admin", "hash", UserRole.Admin, 0, TestFixtures.FixedUtc);
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "hash").Returns(false);

        var act = () => CreateHandler().Handle(new LoginCommand("admin", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
