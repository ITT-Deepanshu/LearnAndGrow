using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Helpers;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class MyProjectsView(ProjectsApi projects, AiApi ai, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("MY PROJECTS");

        try
        {
            var projectList = await projects.ListAsync(ct);
            if (projectList.Count == 0) { ui.WriteError("No projects found."); ui.Pause(); return; }

            ui.PrintTable(
                ["ID", "Project", "End Date", "Health"],
                projectList.Select(p => new List<string>
                {
                    p.Id.ToString(), p.Name, ui.FormatDate(p.EndDate), ui.HealthEmoji(p.Health)
                }));

            var projectId = ProjectListHelper.PromptProjectId(projectList, ui, "Enter project ID or name to view details");
            if (projectId is null) { ui.Pause(); return; }

            await ShowProjectDetailAsync(projectId.Value, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ShowProjectDetailAsync(long projectId, CancellationToken ct)
    {
        while (true)
        {
            try
            {
                var project = await projects.GetAsync(projectId, ct);
                ProjectHealth? health = null;
                try
                {
                    health = await projects.GetHealthAsync(projectId, ct);
                }
                catch (ApiException ex)
                {
                    ui.WriteWarning($"Could not load health details: {ex.Message}");
                }

                ui.ClearScreen();
                ui.DrawSection($"{project.Name} (ID: {project.Id})");
                Console.WriteLine($"Description   : {project.Description}");
                Console.WriteLine($"Status        : {project.Status}");
                Console.WriteLine($"Start Date    : {ui.FormatDate(project.StartDate)}");
                Console.WriteLine($"End Date      : {ui.FormatDate(project.EndDate)}");
                Console.WriteLine($"Manager       : {project.ManagerName}");
                Console.WriteLine($"Story Points  : {project.TotalStoryPoints}");
                Console.WriteLine($"Health Status : {ui.HealthEmoji(project.Health)}");
                if (!string.IsNullOrWhiteSpace(project.HealthReason))
                    Console.WriteLine($"Health Reason : {project.HealthReason}");

                Console.WriteLine();
                Console.WriteLine("Risk Flags:");
                var riskFlags = health?.RiskFlags ?? [];
                if (riskFlags.Count == 0)
                    Console.WriteLine("  (none)");
                else
                {
                    foreach (var flag in riskFlags)
                        Console.WriteLine($"  {(flag.StartsWith("✓") ? "✓" : "✗")}  {flag.TrimStart('✓', '✗', ' ')}");
                }

                Console.WriteLine();
                Console.WriteLine("Milestones:");
                var milestones = project.Milestones ?? [];
                if (milestones.Count == 0)
                    Console.WriteLine("  (none)");
                else
                {
                    ui.PrintTable(
                        ["ID", "Title", "Due Date", "Status"],
                        milestones.Select(m => new List<string>
                        {
                            m.Id.ToString(),
                            m.Title,
                            ui.FormatDate(m.DueDate),
                            m.Status + (IsOverdue(m) ? "  ⚠ OVERDUE" : string.Empty)
                        }));
                }

                Console.WriteLine();
                Console.WriteLine("Allocated Resources:");
                var allocations = project.Allocations ?? [];
                if (allocations.Count == 0)
                    Console.WriteLine("  (none)");
                else
                {
                    ui.PrintTable(
                        ["Name", "%", "From", "To"],
                        allocations.Select(a => new List<string>
                        {
                            a.EmployeeName, $"{a.UtilisationPercentage}%",
                            ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                        }));
                }

                ui.DrawDivider();
                Console.WriteLine("[A] Get AI Risk Summary     [B] Back");
                var action = ui.Prompt("Action").ToUpperInvariant();
                if (action == "B") return;
                if (action == "A")
                    await ShowRiskSummaryAsync(projectId, project.Name, ct);
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); return; }
        }
    }

    private async Task ShowRiskSummaryAsync(long projectId, string projectName, CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawSection($"AI Risk Summary — {projectName}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("Generating AI summary...");
            var summary = await ai.RiskSummaryAsync(projectId, ct);
            Console.WriteLine();
            Console.WriteLine($"\"{summary.Paragraph}\"");
            Console.WriteLine();
            Console.WriteLine("  Note: This summary is AI-generated from milestone and timesheet data.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }

    private static bool IsOverdue(Milestone milestone) =>
        milestone.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) &&
        milestone.DueDate < DateOnly.FromDateTime(DateTime.Today);
}
