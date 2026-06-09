using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Projects.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateProjectCommandHandlerTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _projects.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateUser(UserRole.Manager, id: 5));
        _projects.GetByIdWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ci => TestFixtures.CreateProject(managerId: 5));
    }

    private CreateProjectCommandHandler CreateHandler() => new(
        _projects, _users, _current, _uow, _clock);

    [Fact]
    public async Task Handle_CreatesProjectForAdmin()
    {
        var command = new CreateProjectCommand(
            "Beta Portal",
            "New project",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            5,
            80);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.Should().Be("Alpha Portal");
        _projects.Received(1).Add(Arg.Any<Project>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenManagerRoleInvalid()
    {
        _users.GetByIdAsync(6, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateUser(UserRole.Employee, id: 6));
        var command = new CreateProjectCommand(
            "Gamma",
            "Desc",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            6,
            50);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Manager role*");
    }

    [Fact]
    public async Task Handle_ThrowsWhenProjectNameExists()
    {
        _projects.ExistsByNameAsync("Beta Portal", Arg.Any<CancellationToken>()).Returns(true);
        var command = new CreateProjectCommand(
            "Beta Portal",
            "Desc",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            5,
            50);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
