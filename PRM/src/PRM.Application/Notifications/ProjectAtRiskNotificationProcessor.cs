using Microsoft.Extensions.Logging;
using PRM.Application.Common;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Notifications;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Notifications.Templates;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Application.Notifications;

public sealed class ProjectAtRiskNotificationProcessor(
    IClock clock,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IResourceProfileRepository employeeRepository,
    IProjectRepository projectRepository,
    ISystemConfigRepository systemConfigRepository,
    IUnitOfWork unitOfWork,
    ProjectEffortBuilder effortBuilder,
    ProjectHealthDomainService healthService,
    IAiProviderOrchestrator aiOrchestrator,
    ILogger<ProjectAtRiskNotificationProcessor> logger) : IProjectAtRiskNotificationProcessor
{
    private const long SystemActorId = 0;
    private const int MaxBenchSuggestions = 5;
    private const string DefaultRiskSummary =
        "The project health scheduler flagged this project as at risk based on milestone and effort signals.";

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var utcNow = clock.UtcNow;
        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);

        var projectIds = (await projectRepository.ListActiveWithDetailsAsync(cancellationToken))
            .Where(p => p.Health == HealthStatus.Red && p.AtRiskNotificationSentAt is null)
            .Select(p => p.Id)
            .ToList();

        var benchSuggestions = await BuildBenchSuggestionsAsync(cancellationToken);

        unitOfWork.ClearChangeTracker();

        foreach (var projectId in projectIds)
        {
            var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken);
            if (project is null)
                continue;

            if (project.Health != HealthStatus.Red || project.AtRiskNotificationSentAt is not null)
                continue;

            await ProcessProjectAsync(project, today, utcNow, lastWeekStart, config.MaxWeeklyHours, benchSuggestions, cancellationToken);
        }
    }

    private async Task ProcessProjectAsync(
        Project project,
        DateOnly today,
        DateTime utcNow,
        DateOnly lastWeekStart,
        int maxWeeklyHours,
        IReadOnlyList<string> benchSuggestions,
        CancellationToken cancellationToken)
    {
        if (project.Manager is null || string.IsNullOrWhiteSpace(project.Manager.Email))
        {
            logger.LogWarning(
                "Skipping at-risk email for project {ProjectId} — manager email missing.",
                project.Id);
            return;
        }

        var activeAllocations = project.Allocations.Where(a => a.IsActiveOn(today)).ToList();
        var effortData = await effortBuilder.BuildAsync(
            activeAllocations,
            project.Id,
            lastWeekStart,
            maxWeeklyHours,
            cancellationToken);

        var health = healthService.Evaluate(project, today, effortData);
        var aiSummary = await BuildRiskSummaryParagraphAsync(project, activeAllocations, effortData, cancellationToken);
        var managerName = project.Manager.ResourceProfile?.FullName ?? project.Manager.Username;

        var html = templateRenderer.RenderProjectAtRisk(new ProjectAtRiskEmailModel(
            project.Name,
            managerName,
            health.DisplayLabel,
            project.Milestones
                .OrderBy(m => m.DueDate)
                .Select(m => new ProjectMilestoneEmailModel(m.Title, m.DueDate, m.Status.ToString()))
                .ToList(),
            health.RiskFlags,
            aiSummary,
            benchSuggestions));

        await emailService.SendAsync(
            new EmailMessage(
                project.Manager.Email,
                NotificationSubjects.ProjectAtRisk(project.Name),
                html,
                managerName),
            cancellationToken);

        project.MarkAtRiskNotificationSent(utcNow, SystemActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> BuildBenchSuggestionsAsync(CancellationToken cancellationToken)
    {
        var bench = await employeeRepository.ListAsync(ResourceProfileStatus.Bench, null, null, cancellationToken);
        return bench
            .Where(e => e.Status != ResourceProfileStatus.Inactive && e.User.IsActive)
            .Take(MaxBenchSuggestions)
            .Select(FormatBenchResource)
            .ToList();
    }

    private static string FormatBenchResource(ResourceProfile profile)
    {
        var skills = profile.Skills.Count == 0
            ? "no skills listed"
            : string.Join(", ", profile.Skills.Select(s => s.Name));
        return $"{profile.FullName} ({profile.Department}) — {skills}";
    }

    private async Task<string> BuildRiskSummaryParagraphAsync(
        Project project,
        IReadOnlyList<Allocation> activeAllocations,
        IReadOnlyList<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)> effortData,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await aiOrchestrator.SummarizeRiskAsync(
                new RiskSummaryAiRequest(
                    project.Name,
                    project.Milestones.Select(m => new MilestoneAiContext(m.Title, m.DueDate, m.Status.ToString())).ToList(),
                    activeAllocations.Select(a => $"{a.ResourceProfile.FullName} ({a.UtilisationPercentage}%)").ToList(),
                    effortData.Select(e => new EffortAiContext(e.EmployeeName, e.ExpectedHours, e.LoggedHours)).ToList()),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Paragraph))
                return response.Paragraph;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI risk summary failed for project {ProjectId}; using stored health reason.", project.Id);
        }

        return string.IsNullOrWhiteSpace(project.HealthReason) ? DefaultRiskSummary : project.HealthReason;
    }
}
