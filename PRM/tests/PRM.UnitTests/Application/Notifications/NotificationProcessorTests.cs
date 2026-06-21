using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PRM.Application.Common;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Notifications;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Notifications;
using PRM.Application.Notifications.Templates;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;
using PRM.Domain.Services;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Notifications;

public class TimesheetComplianceNotificationProcessorTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templates = Substitute.For<IEmailTemplateRenderer>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public TimesheetComplianceNotificationProcessorTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _templates.RenderTimesheetReminder(Arg.Any<TimesheetReminderEmailModel>()).Returns("<p>reminder</p>");
        _templates.RenderTimesheetFrozenEmployee(Arg.Any<TimesheetFrozenEmployeeEmailModel>()).Returns("<p>frozen</p>");
        _templates.RenderTimesheetFrozenManager(Arg.Any<TimesheetFrozenManagerEmailModel>()).Returns("<p>manager</p>");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private TimesheetComplianceNotificationProcessor CreateProcessor() => new(
        _clock,
        _email,
        _templates,
        _employees,
        _timesheets,
        _unitOfWork,
        NullLogger<TimesheetComplianceNotificationProcessor>.Instance);

    private void SetupEligibleEmployee(ResourceProfile employee)
    {
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([employee]);
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
    }

    private static void MarkMissedCompliance(ResourceProfile employee, DateOnly weekStart, int reminderCount = 0)
    {
        employee.SyncMissedTimesheetWeek(weekStart, 0, TestFixtures.FixedUtc);
        for (var i = 0; i < reminderCount; i++)
            employee.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);
    }

    [Fact]
    public async Task ProcessAsync_DoesNothingOnDeadlineDay()
    {
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(new DateOnly(2026, 6, 7));
        _clock.Today.Returns(weekStart.AddDays(6));
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns([TestFixtures.CreateResourceProfileWithManager()]);

        await CreateProcessor().ProcessAsync();

        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SendsFirstReminderOnFirstWorkingDay()
    {
        var today = new DateOnly(2026, 6, 8);
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        _clock.Today.Returns(today);

        var employee = TestFixtures.CreateResourceProfileWithManager();
        MarkMissedCompliance(employee, weekStart);
        SetupEligibleEmployee(employee);

        await CreateProcessor().ProcessAsync();

        employee.TimesheetReminderCount.Should().Be(1);
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "employee@example.com" && m.Subject.Contains("reminder 1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SendsSecondReminderOnSecondWorkingDay()
    {
        var today = new DateOnly(2026, 6, 9);
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        _clock.Today.Returns(today);

        var employee = TestFixtures.CreateResourceProfileWithManager();
        MarkMissedCompliance(employee, weekStart, reminderCount: 1);
        SetupEligibleEmployee(employee);

        await CreateProcessor().ProcessAsync();

        employee.TimesheetReminderCount.Should().Be(2);
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Subject.Contains("reminder 2")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FreezesAndNotifiesEmployeeAndManager()
    {
        var today = new DateOnly(2026, 6, 10);
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        _clock.Today.Returns(today);

        var employee = TestFixtures.CreateResourceProfileWithManager();
        MarkMissedCompliance(employee, weekStart, reminderCount: 2);
        SetupEligibleEmployee(employee);

        await CreateProcessor().ProcessAsync();

        employee.TimesheetSubmissionFrozen.Should().BeTrue();
        await _email.Received(2).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SkipsInactiveEmployees()
    {
        var today = new DateOnly(2026, 6, 8);
        _clock.Today.Returns(today);

        var inactive = TestFixtures.CreateResourceProfileWithManager();
        inactive.Deactivate(0, TestFixtures.FixedUtc);
        MarkMissedCompliance(inactive, WeekHelper.GetLastCompletedWeekMonday(today));
        _employees.ListAsync(null, null, null, Arg.Any<CancellationToken>()).Returns([inactive]);

        await CreateProcessor().ProcessAsync();

        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotRefreezeAlreadyFrozenEmployee()
    {
        var today = new DateOnly(2026, 6, 10);
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        _clock.Today.Returns(today);

        var employee = TestFixtures.CreateResourceProfileWithManager();
        MarkMissedCompliance(employee, weekStart, reminderCount: 2);
        employee.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        SetupEligibleEmployee(employee);

        await CreateProcessor().ProcessAsync();

        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}

public class ProjectAtRiskNotificationProcessorTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templates = Substitute.For<IEmailTemplateRenderer>();
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IAiProviderOrchestrator _ai = Substitute.For<IAiProviderOrchestrator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProjectEffortBuilder _effortBuilder;
    private readonly ProjectHealthDomainService _healthService = new();

    public ProjectAtRiskNotificationProcessorTests()
    {
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _clock.Today.Returns(new DateOnly(2026, 6, 10));
        _config.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
        _effortBuilder = new ProjectEffortBuilder(_timesheets);
        _timesheets.ListForTeamWeekAsync(Arg.Any<IReadOnlyList<long>>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _employees.ListAsync(ResourceProfileStatus.Bench, null, null, Arg.Any<CancellationToken>()).Returns([]);
        _templates.RenderProjectAtRisk(Arg.Any<ProjectAtRiskEmailModel>()).Returns("<p>at risk</p>");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private ProjectAtRiskNotificationProcessor CreateProcessor() => new(
        _clock,
        _email,
        _templates,
        _employees,
        _projects,
        _config,
        _unitOfWork,
        _effortBuilder,
        _healthService,
        _ai,
        NullLogger<ProjectAtRiskNotificationProcessor>.Instance);

    private void SetupAtRiskProject(Project project)
    {
        _projects.ListActiveWithDetailsAsync(Arg.Any<CancellationToken>()).Returns([project]);
        _projects.GetByIdWithDetailsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
    }

    [Fact]
    public async Task ProcessAsync_SendsEmailAndMarksNotificationSent()
    {
        var project = TestFixtures.CreateAtRiskProject();
        SetupAtRiskProject(project);
        _ai.SummarizeRiskAsync(Arg.Any<RiskSummaryAiRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RiskSummaryAiResponse("Delivery is at risk."));

        await CreateProcessor().ProcessAsync();

        project.AtRiskNotificationSentAt.Should().NotBeNull();
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "pm@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SkipsWhenManagerEmailMissing()
    {
        var project = TestFixtures.CreateAtRiskProject(managerEmail: " ");
        SetupAtRiskProject(project);

        await CreateProcessor().ProcessAsync();

        project.AtRiskNotificationSentAt.Should().BeNull();
        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FallsBackToHealthReasonWhenAiFails()
    {
        var project = TestFixtures.CreateAtRiskProject();
        SetupAtRiskProject(project);
        _ai.SummarizeRiskAsync(Arg.Any<RiskSummaryAiRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<RiskSummaryAiResponse>>(_ => throw new InvalidOperationException("AI unavailable"));

        await CreateProcessor().ProcessAsync();

        _templates.Received(1).RenderProjectAtRisk(
            Arg.Is<ProjectAtRiskEmailModel>(m => m.AiRiskSummary.Contains("Alpha milestone")));
    }
}
