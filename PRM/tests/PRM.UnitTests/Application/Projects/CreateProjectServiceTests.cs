using FluentAssertions;
using NSubstitute;
using PRM.Application.Common;
using PRM.Application.Projects;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Services;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Projects;

public class CreateProjectServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateProjectServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _projects.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateUser(UserRole.Manager, id: 5));
        _projects.GetByIdWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ci => TestFixtures.CreateProject(managerId: 5));
    }

    private ProjectService CreateService() => new(
        _projects,
        _users,
        Substitute.For<ISystemConfigRepository>(),
        new ProjectEffortBuilder(Substitute.For<ITimesheetRepository>()),
        new ProjectHealthDomainService(),
        _current,
        _uow,
        _clock);

    [Fact]
    public async Task CreateProjectAsync_CreatesProjectForAdmin()
    {
        var dto = new CreateProjectDto(
            "Beta Portal",
            "New project",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            5,
            80);

        var result = await CreateService().CreateProjectAsync(dto, CancellationToken.None);

        result.Name.Should().Be("Alpha Portal");
        _projects.Received(1).Add(Arg.Any<Project>());
    }

    [Fact]
    public async Task CreateProjectAsync_ThrowsWhenManagerRoleInvalid()
    {
        _users.GetByIdAsync(6, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateUser(UserRole.Resource, id: 6));
        var dto = new CreateProjectDto(
            "Gamma",
            "Desc",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            6,
            50);

        var act = () => CreateService().CreateProjectAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Manager role*");
    }

    [Fact]
    public async Task CreateProjectAsync_ThrowsWhenProjectNameExists()
    {
        _projects.ExistsByNameAsync("Beta Portal", Arg.Any<CancellationToken>()).Returns(true);
        var dto = new CreateProjectDto(
            "Beta Portal",
            "Desc",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Planned,
            5,
            50);

        var act = () => CreateService().CreateProjectAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
