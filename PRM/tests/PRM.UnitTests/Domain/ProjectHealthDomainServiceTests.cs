using FluentAssertions;
using PRM.Domain.Enums;
using PRM.Domain.Services;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Domain;

public class ProjectHealthDomainServiceTests
{
    private readonly ProjectHealthDomainService _service = new();

    [Fact]
    public void Evaluate_ReturnsGreenWhenNoIssues()
    {
        var project = TestFixtures.CreateProject();
        var result = _service.Evaluate(project, TestFixtures.FixedToday, []);
        result.Status.Should().Be(HealthStatus.Green);
    }

    [Fact]
    public void Evaluate_ReturnsRedWhenOverdueMilestoneAndLowEffort()
    {
        var project = TestFixtures.CreateProject();
        project.AddMilestone("Backend API", new DateOnly(2026, 4, 1), 40, 1, TestFixtures.FixedUtc);

        var result = _service.Evaluate(
            project,
            TestFixtures.FixedToday,
            [("Ravi Kumar", 20, 4)]);

        result.Status.Should().Be(HealthStatus.Red);
        result.RiskFlags.Should().Contain(f => f.Contains("overdue"));
    }

    [Fact]
    public void Evaluate_ReturnsYellowWhenMildLowEffort()
    {
        var project = TestFixtures.CreateProject();
        var result = _service.Evaluate(
            project,
            TestFixtures.FixedToday,
            [("Neha Joshi", 20, 15)]);

        result.Status.Should().Be(HealthStatus.Yellow);
    }
}
