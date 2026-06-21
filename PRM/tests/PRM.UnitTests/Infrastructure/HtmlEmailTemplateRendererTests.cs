using FluentAssertions;
using PRM.Application.Notifications.Templates;
using PRM.Infrastructure.Email;

namespace PRM.UnitTests.Infrastructure;

public class HtmlEmailTemplateRendererTests
{
    private readonly HtmlEmailTemplateRenderer _renderer = new();

    [Fact]
    public void RenderTimesheetReminder_IncludesEmployeeNameAndReminderNumber()
    {
        var html = _renderer.RenderTimesheetReminder(new TimesheetReminderEmailModel("Test User", new DateOnly(2026, 6, 1), 2));

        html.Should().Contain("Test User");
        html.Should().Contain("reminder <strong>#2</strong>");
        html.Should().Contain("01 Jun 2026");
    }

    [Fact]
    public void RenderTimesheetFrozenEmployee_ExplainsRestrictedSubmission()
    {
        var html = _renderer.RenderTimesheetFrozenEmployee(new TimesheetFrozenEmployeeEmailModel("Test User", new DateOnly(2026, 6, 1)));

        html.Should().Contain("cannot create, update,");
        html.Should().Contain("or submit timesheet entries");
    }

    [Fact]
    public void RenderTimesheetFrozenManager_IncludesEmployeeAndManagerNames()
    {
        var html = _renderer.RenderTimesheetFrozenManager(
            new TimesheetFrozenManagerEmailModel("Test User", "Jane Manager", new DateOnly(2026, 6, 1)));

        html.Should().Contain("Test User");
        html.Should().Contain("Jane Manager");
        html.Should().Contain("restore access");
    }

    [Fact]
    public void RenderProjectAtRisk_IncludesProjectHealthAndAiSummary()
    {
        var html = _renderer.RenderProjectAtRisk(new ProjectAtRiskEmailModel(
            "Alpha Portal",
            "Jane Manager",
            "AT RISK",
            [new ProjectMilestoneEmailModel("Go-live", new DateOnly(2026, 6, 15), "NotStarted")],
            ["Milestone overdue"],
            "Project delivery is slipping.",
            ["Alex Dev (Backend) — C#, SQL"]));

        html.Should().Contain("Alpha Portal");
        html.Should().Contain("AT RISK");
        html.Should().Contain("Go-live");
        html.Should().Contain("Project delivery is slipping.");
        html.Should().Contain("Alex Dev (Backend) — C#, SQL");
    }
}
