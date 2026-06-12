using FluentAssertions;
using NSubstitute;
using PRM.Application.Ai;
using PRM.Application.Common;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Services;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Ai;

public class FindResourcesServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IAiProviderOrchestrator _ai = Substitute.For<IAiProviderOrchestrator>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public FindResourcesServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        _clock.Today.Returns(TestFixtures.FixedToday);
        _projects.GetByIdAsync(201, Arg.Any<CancellationToken>())
            .Returns(TestFixtures.CreateProject(managerId: 1, id: 201));
        _config.GetAsync(Arg.Any<CancellationToken>())
            .Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _timesheets.GetRecentActivityTagsForEmployeeAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private AiService CreateService() => new(
        _projects,
        _employees,
        _allocations,
        _timesheets,
        _config,
        new ProjectEffortBuilder(_timesheets),
        new ProjectHealthDomainService(),
        _ai,
        _current,
        _clock);

    [Fact]
    public async Task FindResourcesAsync_ExcludesManagerAndAdminFromCandidates()
    {
        var resource = TestFixtures.CreateResourceProfile(userId: 2, id: 10, status: ResourceProfileStatus.Bench);
        var managerProfile = TestFixtures.CreateResourceProfile(userId: 3, id: 11, status: ResourceProfileStatus.Bench);
        TestFixtures.SetProperty(managerProfile, "User", TestFixtures.CreateUser(UserRole.Manager, id: 3));

        _employees.ListAsync(ResourceProfileStatus.Bench, null, null, Arg.Any<CancellationToken>())
            .Returns([resource, managerProfile]);
        _employees.ListAsync(ResourceProfileStatus.PartiallyAllocated, null, null, Arg.Any<CancellationToken>())
            .Returns([]);

        SkillMatchAiRequest? captured = null;
        _ai.MatchSkillsAsync(Arg.Do<SkillMatchAiRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new SkillMatchAiResponse([
                new RankedCandidateAiResult(10, "Ravi", "Good fit", 50)
            ]));

        var result = await CreateService().FindResourcesAsync(201, "Need Java developer", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Candidates.Should().ContainSingle(c => c.ResourceProfileId == 10);
        result.Candidates.Should().ContainSingle(c => c.ResourceProfileId == 10);
    }

    [Fact]
    public async Task FindResourcesAsync_ReturnsNoteWhenNoEligibleCapacity()
    {
        _employees.ListAsync(ResourceProfileStatus.Bench, null, null, Arg.Any<CancellationToken>()).Returns([]);
        _employees.ListAsync(ResourceProfileStatus.PartiallyAllocated, null, null, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateService().FindResourcesAsync(201, "Need QA", CancellationToken.None);

        result.Candidates.Should().BeEmpty();
        result.Note.Should().Contain("resource-role");
        await _ai.DidNotReceive().MatchSkillsAsync(Arg.Any<SkillMatchAiRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindResourcesAsync_ThrowsWhenRequirementEmpty()
    {
        var act = () => CreateService().FindResourcesAsync(201, "  ", CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task FindResourcesAsync_ThrowsWhenManagerDoesNotOwnProject()
    {
        _current.UserId.Returns(99L);
        var act = () => CreateService().FindResourcesAsync(201, "Need dev", CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
