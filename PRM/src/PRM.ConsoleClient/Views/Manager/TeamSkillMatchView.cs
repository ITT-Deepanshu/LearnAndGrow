using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class TeamSkillMatchView(AiApi ai, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("TEAM BUILDER WITH SKILL MATCH");
            Console.WriteLine("Define your whole project team in one request.");
            Console.WriteLine("The system loads bench data first, then asks the AI to match every role at once.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  I have a new project where I need 2 backend developers,");
            Console.WriteLine("  1 frontend developer and 1 QA.");
            Console.WriteLine();
            Console.WriteLine("Enter your team requirement (or B to go back):");
            var requirement = ui.Prompt("> ");
            if (requirement.Equals("B", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.IsNullOrWhiteSpace(requirement))
            {
                ui.WriteError("Requirement cannot be empty.");
                ui.Pause();
                continue;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("Searching... (loading employee data, matching team in one pass)");
                var result = await ai.TeamSkillMatchAsync(requirement, ct);
                PrintResult(result);
            }
            catch (ApiException ex)
            {
                ui.WriteError(ex.Message);
            }

            ui.Pause();
        }
    }

    private void PrintResult(TeamSkillMatchResult result)
    {
        Console.WriteLine();
        ui.DrawBox("TEAM SKILL MATCH");
        Console.WriteLine($"Request: \"{result.Requirement}\"");
        Console.WriteLine();

        if (result.TeamDefined.Count > 0)
        {
            Console.WriteLine("Team defined:");
            Console.WriteLine();
            foreach (var role in result.TeamDefined)
            {
                var skillText = role.RequiredSkills.Count == 0
                    ? "skills inferred from request"
                    : string.Join(", ", role.RequiredSkills.Select(s => $"{s.Name}, min {s.MinProficiency}"));
                Console.WriteLine($"  - {role.Count}x {role.RoleTitle} ({skillText})");
            }
            Console.WriteLine();
        }

        if (result.Assignments.Count > 0)
        {
            Console.WriteLine("Assigned from bench:");
            Console.WriteLine();
            foreach (var assignment in result.Assignments.OrderBy(a => a.RoleTitle).ThenBy(a => a.SlotNumber))
            {
                Console.WriteLine($"  {assignment.RoleTitle} #{assignment.SlotNumber}: {assignment.EmployeeName}");
                Console.WriteLine($"      Manager: {assignment.ManagerName}");
                Console.WriteLine($"      Skills: {string.Join(", ", assignment.Skills)}");
                Console.WriteLine($"      Why: {assignment.Why}");
                Console.WriteLine();
            }
        }

        if (result.Unfilled.Count > 0)
        {
            Console.WriteLine("Could not fill:");
            Console.WriteLine();
            foreach (var gap in result.Unfilled)
            {
                Console.WriteLine($"  {gap.RoleTitle} - {gap.UnfilledCount} unfilled");
                Console.WriteLine($"      Why: {gap.Reason}");
                if (!string.IsNullOrWhiteSpace(gap.Detail))
                    Console.WriteLine($"      {gap.Detail}");
                Console.WriteLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(result.Note))
            Console.WriteLine($"Note: {result.Note}");
    }
}
