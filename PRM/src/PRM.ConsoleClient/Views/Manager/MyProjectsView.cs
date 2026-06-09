using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class MyProjectsView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("MY PROJECTS");

        try
        {
            var projects = await api.ListProjectsAsync(ct);
            if (projects.Count == 0) { ui.WriteError("No projects found."); ui.Pause(); return; }

            ui.PrintTable(
                ["#", "Project", "End Date", "Health"],
                projects.Select((p, i) => new List<string>
                {
                    $"{i + 1}.", p.Name, ui.FormatDate(p.EndDate), ui.HealthEmoji(p.Health)
                }));

            var choice = ui.PromptInt("Select project number to view details", 1, projects.Count) - 1;
            await ShowProjectDetailAsync(projects[choice].Id, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ShowProjectDetailAsync(long projectId, CancellationToken ct)
    {
        while (true)
        {
            try
            {
                var project = await api.GetProjectAsync(projectId, ct);
                var health = await api.GetProjectHealthAsync(projectId, ct);

                ui.ClearScreen();
                ui.DrawSection(project.Name);
                Console.WriteLine($"Health Status : {ui.HealthEmoji(project.Health)}");
                Console.WriteLine();
                Console.WriteLine("Risk Flags:");
                foreach (var flag in health.RiskFlags)
                    Console.WriteLine($"  {(flag.StartsWith("✓") ? "✓" : "✗")}  {flag.TrimStart('✓', '✗', ' ')}");

                Console.WriteLine();
                Console.WriteLine("Milestones:");
                ui.PrintTable(
                    ["#", "Title", "Due Date", "Status"],
                    project.Milestones.Select((m, i) => new List<string>
                    {
                        $"{i + 1}.", m.Title, ui.FormatDate(m.DueDate),
                        m.Status + (IsOverdue(m) ? "  ⚠ OVERDUE" : string.Empty)
                    }));

                Console.WriteLine();
                Console.WriteLine("Allocated Resources:");
                ui.PrintTable(
                    ["Name", "%", "From", "To"],
                    project.Allocations.Select(a => new List<string>
                    {
                        a.EmployeeName, $"{a.UtilisationPercentage}%",
                        ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                    }));

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
            var summary = await api.RiskSummaryAsync(projectId, ct);
            Console.WriteLine();
            Console.WriteLine($"\"{summary.Paragraph}\"");
            Console.WriteLine();
            Console.WriteLine("  Note: This summary is AI-generated from milestone and timesheet data.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }

    private static bool IsOverdue(Models.Milestone milestone) =>
        milestone.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) &&
        milestone.DueDate < DateOnly.FromDateTime(DateTime.Today);
}
