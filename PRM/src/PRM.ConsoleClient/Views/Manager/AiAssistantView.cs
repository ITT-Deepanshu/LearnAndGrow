using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class AiAssistantView(ApiClient api, ConsoleUi ui, AllocateResourceView allocateView)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("AI ASSISTANT");
            Console.WriteLine("1. Skill Match    — Find best employees for a project requirement");
            Console.WriteLine("2. Risk Summary   — Get a health analysis for a project");
            Console.WriteLine("3. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await SkillMatchAsync(ct); break;
                case "2": await RiskSummaryAsync(ct); break;
                case "3": return;
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
            var projects = await api.ListProjectsAsync(ct);
            if (projects.Count == 0) { ui.WriteError("No projects available."); ui.Pause(); return; }

            foreach (var p in projects)
                Console.WriteLine($"  {p.Id}. {p.Name}");
            var projectId = ui.PromptLong("Select project ID");

            Console.WriteLine();
            Console.WriteLine("Describe your project requirement in plain English:");
            var requirement = ui.Prompt("> ");
            Console.WriteLine();
            Console.WriteLine("Searching... (calling AI)");

            var result = await api.SkillMatchAsync(projectId, requirement, ct);
            Console.WriteLine();
            Console.WriteLine("Results:");
            for (var i = 0; i < result.Candidates.Count; i++)
            {
                var c = result.Candidates[i];
                Console.WriteLine($"  {i + 1}.  {c.Name}");
                Console.WriteLine($"      Reason: {c.Reason}");
                Console.WriteLine();
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
            var projects = await api.ListProjectsAsync(ct);
            if (projects.Count == 0) { ui.WriteError("No projects available."); ui.Pause(); return; }

            Console.WriteLine("Select project:");
            for (var i = 0; i < projects.Count; i++)
                Console.WriteLine($"  {i + 1}.  {projects[i].Name}    {ui.HealthEmoji(projects[i].Health)}");

            var choice = ui.PromptInt("Enter project number", 1, projects.Count) - 1;
            var project = projects[choice];

            Console.WriteLine();
            Console.WriteLine("Generating AI summary...");
            var summary = await api.RiskSummaryAsync(project.Id, ct);
            Console.WriteLine();
            Console.WriteLine($"\"{summary.Paragraph}\"");
            Console.WriteLine();
            Console.WriteLine("  Note: AI-generated from current milestone and timesheet data.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }
}
