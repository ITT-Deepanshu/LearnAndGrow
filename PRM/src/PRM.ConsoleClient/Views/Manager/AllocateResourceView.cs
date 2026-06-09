using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class AllocateResourceView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("ALLOCATE RESOURCE");
            Console.WriteLine("1. Find resource using AI (recommended)");
            Console.WriteLine("2. Allocate directly (I already know who I want)");
            Console.WriteLine("3. End an existing allocation");
            Console.WriteLine("4. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await AiAllocateAsync(ct); break;
                case "2": await DirectAllocateAsync(ct); break;
                case "3": await EndAllocationAsync(ct); break;
                case "4": return;
                default: ui.WriteError("Invalid option."); ui.Pause(); break;
            }
        }
    }

    private async Task<long?> SelectProjectAsync(CancellationToken ct)
    {
        var projects = await api.ListProjectsAsync(ct);
        if (projects.Count == 0) { ui.WriteError("No projects available."); return null; }

        foreach (var p in projects)
            Console.WriteLine($"  {p.Id}. {p.Name} ({p.Status})");

        var input = ui.Prompt("Enter project name or ID");
        var project = long.TryParse(input, out var id)
            ? projects.FirstOrDefault(p => p.Id == id)
            : projects.FirstOrDefault(p => p.Name.Contains(input, StringComparison.OrdinalIgnoreCase));

        if (project is null) { ui.WriteError("Project not found."); return null; }
        Console.WriteLine($"Selected: {project.Name} ({project.Id})");
        return project.Id;
    }

    private async Task AiAllocateAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("ALLOCATE RESOURCE");

        try
        {
            Console.WriteLine("Step 1 — Select Project");
            var projectId = await SelectProjectAsync(ct);
            if (projectId is null) { ui.Pause(); return; }

            Console.WriteLine();
            Console.WriteLine("Step 2 — Describe your requirement");
            Console.WriteLine("Type what kind of resource you need:");
            var requirement = ui.Prompt("> ");
            Console.WriteLine();
            Console.WriteLine("Searching... (AI matching in progress)");
            Console.WriteLine();

            var result = await api.SkillMatchAsync(projectId.Value, requirement, ct);
            if (result.Candidates.Count == 0)
            {
                ui.WriteWarning("No matching resources found.");
                ui.Pause();
                return;
            }

            Console.WriteLine("AI-MATCHED RESULTS");
            ui.DrawDivider();
            ui.PrintTable(
                ["#", "Name", "Reason", "Suggested %"],
                result.Candidates.Select((c, i) => new List<string>
                {
                    (i + 1).ToString(), c.Name, Truncate(c.Reason, 40), c.SuggestedUtilisation?.ToString() ?? "-"
                }));

            if (!string.IsNullOrWhiteSpace(result.Note))
                Console.WriteLine($"Note: {result.Note}");
            ui.DrawDivider();

            var choice = ui.PromptInt("Select employee (enter #, or 0 to cancel)", 0, result.Candidates.Count);
            if (choice == 0) return;

            var candidate = result.Candidates[choice - 1];
            await ConfirmAllocationAsync(projectId.Value, candidate.EmployeeId, candidate.Name, candidate.SuggestedUtilisation, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task DirectAllocateAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("DIRECT ALLOCATION");

        try
        {
            Console.WriteLine("Step 1 — Select Project");
            var projectId = await SelectProjectAsync(ct);
            if (projectId is null) { ui.Pause(); return; }

            var employeeId = ui.PromptLong("Enter Employee ID");
            var employee = await api.GetEmployeeAsync(employeeId, ct);
            await ConfirmAllocationAsync(projectId.Value, employeeId, employee.FullName, null, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ConfirmAllocationAsync(
        long projectId, long employeeId, string employeeName, decimal? suggestedUtilisation, CancellationToken ct)
    {
        try
        {
            var allocations = await api.ListAllocationsByEmployeeAsync(employeeId, ct);
            var activeUtil = allocations.Where(a => a.EndedAt is null).Sum(a => a.UtilisationPercentage);

            ui.DrawSection(employeeName);
            Console.WriteLine($"Current Utilisation: {activeUtil}%");

            var utilisation = suggestedUtilisation ?? ui.PromptDecimal("Utilisation %");
            var fromDate = ui.PromptDate("From Date (DD-MM-YYYY)");
            var toDate = ui.PromptDate("To Date (DD-MM-YYYY)");

            Console.WriteLine();
            Console.WriteLine("Validating...");
            Console.WriteLine($"  {employeeName} total in this period: {activeUtil}% + {utilisation}% = {activeUtil + utilisation}%");
            ui.DrawDivider();

            if (ui.Prompt("Action [C] Confirm  [B] Back").ToUpperInvariant() != "C") return;

            var project = (await api.ListProjectsAsync(ct)).First(p => p.Id == projectId);
            await api.CreateAllocationAsync(new CreateAllocationRequest(
                projectId, employeeId, utilisation, fromDate, toDate), ct);
            ui.WriteSuccess($"Allocation saved. {employeeName} → {project.Name} ({utilisation}%, {ui.FormatDate(fromDate)}–{ui.FormatDate(toDate)})");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task EndAllocationAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("END ALLOCATION");

        try
        {
            var projectId = await SelectProjectAsync(ct);
            if (projectId is null) { ui.Pause(); return; }

            var allocations = (await api.ListAllocationsByProjectAsync(projectId.Value, ct))
                .Where(a => a.EndedAt is null).ToList();

            if (allocations.Count == 0) { ui.WriteError("No active allocations on this project."); ui.Pause(); return; }

            Console.WriteLine("Active Allocations on this project:");
            ui.PrintTable(
                ["#", "Employee", "%", "From", "To"],
                allocations.Select((a, i) => new List<string>
                {
                    $"{i + 1}.", a.EmployeeName, $"{a.UtilisationPercentage}%",
                    ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                }));

            var choice = ui.PromptInt("Select allocation to end", 1, allocations.Count) - 1;
            var selected = allocations[choice];
            var today = DateOnly.FromDateTime(DateTime.Today);

            Console.WriteLine($"End {selected.EmployeeName}'s allocation on {selected.ProjectName}?");
            Console.WriteLine($"Set end date to today ({ui.FormatDate(today)})?");
            if (!ui.Confirm("[Y] Yes, End Now")) return;

            await api.EndAllocationAsync(selected.Id, ct);
            ui.WriteSuccess($"Allocation ended. {selected.EmployeeName} freed from {selected.ProjectName} as of {ui.FormatDate(today)}.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 3)] + "...";
}
