using FluentAssertions;
using NSubstitute;
using PRM.Application.Auth;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using ResourceProfile = PRM.Domain.Entities.ResourceProfile;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Auth;

public class LoginServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public LoginServiceTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokens.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IReadOnlyList<string>>()).Returns("access");
    }

    private AuthService CreateService() => new(
        _users, _audit, _hasher, _tokens, Substitute.For<ICurrentUser>(), _uow, _clock);

    [Fact]
    public async Task LoginAsync_ReturnsTokensOnValidLogin()
    {
        var user = TestFixtures.CreateUser(UserRole.Admin);
        var profile = ResourceProfile.Create(user.Id, "Admin", "", "", 0, TestFixtures.FixedUtc);
        TestFixtures.SetId(profile, 99);
        TestFixtures.SetProperty(user, "ResourceProfile", profile);
        _users.GetByUsernameAsync("test.user", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateService().LoginAsync(new LoginDto("test.user", "Admin@1234"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access");
        result.Value.Role.Should().Be("ADMIN");
        result.Value.ResourceProfileId.Should().Be(99);
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailureWhenUserNotFound()
    {
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateService().LoginAsync(new LoginDto("unknown", "x"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailureWhenAccountDisabled()
    {
        var user = TestFixtures.CreateUser(UserRole.Resource, isActive: false);
        _users.GetByUsernameAsync("test.user", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateService().LoginAsync(new LoginDto("test.user", "x"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.AccountDisabled);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailureWhenPasswordInvalid()
    {
        var user = TestFixtures.CreateUser(UserRole.Admin);
        _users.GetByUsernameAsync("test.user", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "hash").Returns(false);

        var result = await CreateService().LoginAsync(new LoginDto("test.user", "wrong"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
    }
}
