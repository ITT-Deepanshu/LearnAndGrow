using FluentAssertions;
using NSubstitute;
using PRM.Application.Users;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Users;

public class CreateUserServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateUserServiceTests()
    {
        _current.UserId.Returns(1L);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _users.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    private UserService CreateService() => new(
        _users, _employees, _audit, _hasher, _current, _uow, _clock);

    [Fact]
    public async Task CreateUserAsync_CreatesEmployeeProfileForEmployeeRole()
    {
        var dto = new CreateUserDto("Ravi Kumar", "ravi@prm.local", "ravi.kumar", "TempPass1", "resource");
        User? capturedUser = null;
        _users.When(x => x.Add(Arg.Any<User>())).Do(ci =>
        {
            capturedUser = ci.Arg<User>();
            TestFixtures.SetId(capturedUser, 99);
            TestFixtures.SetProperty(capturedUser, "Role", Role.Create((long)UserRole.Resource, "resource", "Resource role"));
        });
        _users.GetByIdWithDetailsAsync(99, Arg.Any<CancellationToken>())
            .Returns(_ => capturedUser);

        var result = await CreateService().CreateUserAsync(dto, UserRole.Resource, CancellationToken.None);

        result.Username.Should().Be("ravi.kumar");
        _employees.Received(1).Add(Arg.Any<ResourceProfile>());
        await _uow.Received().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsWhenUsernameExists()
    {
        _users.ExistsByUsernameAsync("existing", Arg.Any<CancellationToken>()).Returns(true);
        var dto = new CreateUserDto("Existing User", "e@p.local", "existing", "TempPass1", "admin");

        var act = () => CreateService().CreateUserAsync(dto, UserRole.Admin, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsWhenNotAuthenticated()
    {
        _current.UserId.Returns((long?)null);
        var dto = new CreateUserDto("X User", "x@p.local", "x", "TempPass1", "admin");

        var act = () => CreateService().CreateUserAsync(dto, UserRole.Admin, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
