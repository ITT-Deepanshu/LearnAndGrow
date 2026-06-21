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

public class TeamSkillMatchServiceTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAllocationRepository _allocations = Substitute.For<IAllocationRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IAiProviderOrchestrator _ai = Substitute.For<IAiProviderOrchestrator>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public TeamSkillMatchServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);
        _clock.Today.Returns(TestFixtures.FixedToday);
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
    public async Task MatchTeamAsync_LoadsBenchDataAndSendsToAiWithoutDbAccess()
    {
        var devOps = CreateBenchEmployee(10, "Deepanshu", "Tom", [("docker", SkillProficiency.Beginner)]);
        var frontend = CreateBenchEmployee(11, "Utkarsh Sharma", null, [("React", SkillProficiency.Intermediate)]);

        _employees.ListAsync(ResourceProfileStatus.Bench, null, null, Arg.Any<CancellationToken>())
            .Returns([devOps, frontend]);

        TeamSkillMatchAiRequest? captured = null;
        _ai.MatchTeamAsync(Arg.Do<TeamSkillMatchAiRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new TeamSkillMatchAiResponse(
                [
                    new TeamRoleDefinitionAiResult("DevOps Engineer", 1,
                        [new RequiredSkillAiResult("Docker", "Beginner")])
                ],
                [
                    new TeamAssignmentAiResult("DevOps Engineer", 1, 10,
                        "Deepanshu has docker skills at Beginner Proficiency.")
                ],
                [
                    new UnfilledRoleAiResult("Backend Developer", 2, "Not enough matching bench resources",
                        "Only 0 bench resource(s) match Java, Spring, min Intermediate.")
                ]));

        var result = await CreateService().MatchTeamAsync(
            "i need a team of two backend engineers, one devops engineer and one frontend engineer",
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RequirementText.Should().Contain("backend engineers");
        captured.BenchEmployees.Should().HaveCount(2);
        captured.BenchEmployees.Should().Contain(c => c.ResourceProfileId == 10 && c.ManagerName == "Tom");
        captured.BenchEmployees.Should().Contain(c => c.ResourceProfileId == 11 && c.ManagerName == "Unassigned");

        result.Assignments.Should().ContainSingle(a => a.EmployeeName == "Deepanshu");
        result.Unfilled.Should().ContainSingle(u => u.RoleTitle == "Backend Developer");
        result.Note.Should().Contain("could not be filled");
    }

    [Fact]
    public async Task MatchTeamAsync_ThrowsWhenRequirementEmpty()
    {
        var act = () => CreateService().MatchTeamAsync("  ", CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task MatchTeamAsync_ReturnsNoteWhenNoBenchEmployees()
    {
        _employees.ListAsync(ResourceProfileStatus.Bench, null, null, Arg.Any<CancellationToken>()).Returns([]);
        _ai.MatchTeamAsync(Arg.Any<TeamSkillMatchAiRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TeamSkillMatchAiResponse([], [], []));

        var result = await CreateService().MatchTeamAsync("need devops", CancellationToken.None);

        result.Note.Should().Contain("on bench");
        await _ai.Received(1).MatchTeamAsync(
            Arg.Is<TeamSkillMatchAiRequest>(r => r.BenchEmployees.Count == 0 && r.AllocatedEmployees.Count == 0),
            Arg.Any<CancellationToken>());
    }

    private static ResourceProfile CreateBenchEmployee(
        long id,
        string name,
        string? managerName,
        IReadOnlyList<(string Skill, SkillProficiency Proficiency)> skills)
    {
        var profile = TestFixtures.CreateResourceProfile(userId: id + 100, id: id, status: ResourceProfileStatus.Bench);
        TestFixtures.SetProperty(profile, "FullName", name);

        if (managerName is not null)
        {
            var managerUser = TestFixtures.CreateUser(UserRole.Manager, id: id + 200);
            var managerProfile = TestFixtures.CreateResourceProfile(userId: id + 200, id: id + 300);
            TestFixtures.SetProperty(managerProfile, "FullName", managerName);
            TestFixtures.SetProperty(managerUser, "ResourceProfile", managerProfile);
            TestFixtures.SetProperty(profile, "Manager", managerUser);
        }

        foreach (var (skill, proficiency) in skills)
            profile.AddSkill(skill, SkillCategory.Other, proficiency, 1, TestFixtures.FixedUtc);

        return profile;
    }
}
