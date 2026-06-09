using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Users.Commands;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Users;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateUserCommandHandlerTests()
    {
        _current.UserId.Returns(1L);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _users.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    private CreateUserCommandHandler CreateHandler() => new(
        _users, _employees, _audit, _hasher, _current, _uow, _clock);

    [Fact]
    public async Task Handle_CreatesEmployeeProfileForEmployeeRole()
    {
        var command = new CreateUserCommand("Ravi Kumar", "ravi@prm.local", "ravi.kumar", "TempPass1", UserRole.Employee);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Username.Should().Be("ravi.kumar");
        _employees.Received(1).Add(Arg.Any<Employee>());
        await _uow.Received().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenUsernameExists()
    {
        _users.ExistsByUsernameAsync("existing", Arg.Any<CancellationToken>()).Returns(true);
        var command = new CreateUserCommand("Existing User", "e@p.local", "existing", "TempPass1", UserRole.Admin);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenNotAuthenticated()
    {
        _current.UserId.Returns((long?)null);
        var command = new CreateUserCommand("X User", "x@p.local", "x", "TempPass1", UserRole.Admin);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
