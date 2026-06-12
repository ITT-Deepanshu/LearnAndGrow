using FluentAssertions;
using NSubstitute;
using PRM.Application.Common;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Projects;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Services;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Projects;

public class ProjectQueryServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ProjectQueryServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
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
    public async Task ListProjectsAsync_FiltersByManager()
    {
        var project = TestFixtures.CreateProject(managerId: 1, id: 301);
        _projects.ListAsync(1L, Arg.Any<CancellationToken>()).Returns([project]);

        var result = await CreateService().ListProjectsAsync(CancellationToken.None);

        result.Should().ContainSingle(p => p.Id == 301);
    }

    [Fact]
    public async Task UpdateProjectAsync_UpdatesWhenAdminAndManagerValid()
    {
        _current.Role.Returns(UserRole.Admin);
        var project = TestFixtures.CreateProject(managerId: 5, id: 302);
        _projects.GetByIdAsync(302, Arg.Any<CancellationToken>()).Returns(project);
        _users.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(TestFixtures.CreateUser(UserRole.Manager, id: 5));

        var dto = new UpdateProjectDto(
            "Updated Name",
            "Updated desc",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Active,
            5,
            100);

        await CreateService().UpdateProjectAsync(302, dto, CancellationToken.None);

        project.Name.Should().Be("Updated Name");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectByIdAsync_ThrowsWhenManagerCannotView()
    {
        _current.UserId.Returns(99L);
        var project = TestFixtures.CreateProject(managerId: 1, id: 303);
        _projects.GetByIdWithDetailsAsync(303, Arg.Any<CancellationToken>()).Returns(project);

        var act = () => CreateService().GetProjectByIdAsync(303, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
