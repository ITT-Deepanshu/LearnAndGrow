using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Helpers;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class AiAssistantView(ProjectsApi projects, AiApi ai, ConsoleUi ui, AllocateResourceView allocateView, TeamSkillMatchView teamSkillMatch)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("AI ASSISTANT");
            Console.WriteLine("1. Skill Match    — Find best employees for a project requirement");
            Console.WriteLine("2. Team Skill Match — Define a whole team from one request");
            Console.WriteLine("3. Risk Summary   — Get a health analysis for a project");
            Console.WriteLine("4. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await SkillMatchAsync(ct); break;
                case "2": await teamSkillMatch.RunAsync(ct); break;
                case "3": await RiskSummaryAsync(ct); break;
                case "4": return;
                default: ui.WriteError("Invalid option."); ui.Pause(); break;
            }
        }
    }

    private async Task SkillMatchAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawSection("Skill Match");

        try
        {
            var projectList = await projects.ListAsync(ct);
            if (projectList.Count == 0) { ui.WriteError("No projects available."); ui.Pause(); return; }

            ui.PrintTable(
                ["ID", "Project", "Status", "Health"],
                projectList.Select(p => new List<string>
                {
                    p.Id.ToString(), p.Name, p.Status, ui.HealthEmoji(p.Health)
                }));
            var projectId = ProjectListHelper.PromptProjectId(projectList, ui, "Enter project ID or name");
            if (projectId is null) { ui.Pause(); return; }

            Console.WriteLine();
            Console.WriteLine("Describe your project requirement in plain English:");
            var requirement = ui.Prompt("> ");
            Console.WriteLine();
            Console.WriteLine("Searching... (calling AI)");

            var result = await ai.SkillMatchAsync(projectId.Value, requirement, ct);
            var matches = result.Candidates.Where(c => c.SuggestedUtilisation is > 0).ToList();
            Console.WriteLine();
            Console.WriteLine("Results:");
            if (matches.Count == 0)
            {
                ui.WriteWarning("No resources with suggested utilisation above 0%.");
            }
            else
            {
                foreach (var c in matches)
                {
                    Console.WriteLine($"  Profile ID {c.ResourceProfileId}: {c.Name}  (Suggested: {c.SuggestedUtilisation}%)");
                    Console.WriteLine($"      Reason: {c.Reason}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("  Note: These are AI-generated suggestions. Always verify availability");
            Console.WriteLine("  and skills with the employee before allocating.");
            ui.DrawDivider();
            var action = ui.Prompt("Action [A] Go to Allocate Resource  [B] Back").ToUpperInvariant();
            if (action == "A")
                await allocateView.RunAsync(ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task RiskSummaryAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawSection("Risk Summary");

        try
        {
            var projectList = await projects.ListAsync(ct);
            if (projectList.Count == 0) { ui.WriteError("No projects available."); ui.Pause(); return; }

            ui.PrintTable(
                ["ID", "Project", "End Date", "Health"],
                projectList.Select(p => new List<string>
                {
                    p.Id.ToString(), p.Name, ui.FormatDate(p.EndDate), ui.HealthEmoji(p.Health)
                }));

            var projectId = ProjectListHelper.PromptProjectId(projectList, ui, "Enter project ID or name");
            if (projectId is null) { ui.Pause(); return; }
            var project = projectList.First(p => p.Id == projectId.Value);

            Console.WriteLine();
            Console.WriteLine("Generating AI summary...");
            var summary = await ai.RiskSummaryAsync(project.Id, ct);
            Console.WriteLine();
            Console.WriteLine($"\"{summary.Paragraph}\"");
            Console.WriteLine();
            Console.WriteLine("  Note: AI-generated from current milestone and timesheet data.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }
}
