using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Helpers;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class AllocateResourceView(
    ProjectsApi projects,
    EmployeesApi employees,
    AllocationsApi allocations,
    AiApi ai,
    ConsoleUi ui)
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
        var projectList = await projects.ListAsync(ct);
        if (projectList.Count == 0) { ui.WriteError("No projects available."); return null; }

        ui.PrintTable(
            ["ID", "Project", "Status", "End Date"],
            projectList.Select(p => new List<string>
            {
                p.Id.ToString(), p.Name, p.Status, ui.FormatDate(p.EndDate)
            }));

        var projectId = ProjectListHelper.PromptProjectId(projectList, ui, "Enter project ID or name");
        if (projectId is null) return null;

        var project = projectList.First(p => p.Id == projectId.Value);
        Console.WriteLine($"Selected: {project.Name} (ID: {project.Id})");
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

            var result = await ai.SkillMatchAsync(projectId.Value, requirement, ct);
            var matches = result.Candidates
                .Where(c => c.SuggestedUtilisation is > 0)
                .ToList();

            if (matches.Count == 0)
            {
                ui.WriteWarning("No matching resources found with suggested utilisation above 0%.");
                ui.Pause();
                return;
            }

            Console.WriteLine("AI-MATCHED RESULTS");
            ui.DrawDivider();
            PrintAiMatchResults(matches);

            if (!string.IsNullOrWhiteSpace(result.Note))
                Console.WriteLine($"Note: {result.Note}");
            ui.DrawDivider();

            var profileInput = ui.Prompt("Enter resource profile ID (or 0 to cancel)");
            if (profileInput == "0") return;

            if (!long.TryParse(profileInput, out var profileId))
            {
                ui.WriteError("Please enter a valid resource profile ID.");
                return;
            }

            var candidate = matches.FirstOrDefault(c => c.ResourceProfileId == profileId);
            if (candidate is null)
            {
                ui.WriteError("Resource profile not found in AI results.");
                return;
            }
            await ConfirmAllocationAsync(projectId.Value, candidate.ResourceProfileId, candidate.Name, candidate.SuggestedUtilisation, ct);
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

            var employeeId = ui.PromptLong("Enter resourceProfile ID");
            var resourceProfile = await employees.GetAsync(employeeId, ct);
            await ConfirmAllocationAsync(projectId.Value, employeeId, resourceProfile.FullName, null, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ConfirmAllocationAsync(
        long projectId, long employeeId, string employeeName, decimal? suggestedUtilisation, CancellationToken ct)
    {
        try
        {
            var allocationList = await allocations.ListByEmployeeAsync(employeeId, ct);
            var activeUtil = allocationList.Where(a => a.EndedAt is null).Sum(a => a.UtilisationPercentage);

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

            var project = (await projects.ListAsync(ct)).First(p => p.Id == projectId);
            await allocations.CreateAsync(new CreateAllocationRequest(
                ProjectId: projectId,
                ResourceProfileId: employeeId,
                UtilisationPercentage: utilisation,
                FromDate: fromDate,
                ToDate: toDate), ct);
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

            var allocationList = (await allocations.ListByProjectAsync(projectId.Value, ct))
                .Where(a => a.EndedAt is null).ToList();

            if (allocationList.Count == 0) { ui.WriteError("No active allocations on this project."); ui.Pause(); return; }

            Console.WriteLine("Active Allocations on this project:");
            ui.PrintTable(
                ["Allocation ID", "Resource", "%", "From", "To"],
                allocationList.Select(a => new List<string>
                {
                    a.Id.ToString(), a.EmployeeName, $"{a.UtilisationPercentage}%",
                    ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                }));

            var allocationId = ui.PromptLong("Enter allocation ID to end");
            var selected = allocationList.FirstOrDefault(a => a.Id == allocationId);
            if (selected is null)
            {
                ui.WriteError("Allocation not found.");
                ui.Pause();
                return;
            }
            var today = DateOnly.FromDateTime(DateTime.Today);

            Console.WriteLine($"End {selected.EmployeeName}'s allocation on {selected.ProjectName}?");
            Console.WriteLine($"Set end date to today ({ui.FormatDate(today)})?");
            if (!ui.Confirm("[Y] Yes, End Now")) return;

            await allocations.EndAsync(selected.Id, ct);
            ui.WriteSuccess($"Allocation ended. {selected.EmployeeName} freed from {selected.ProjectName} as of {ui.FormatDate(today)}.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private static void PrintAiMatchResults(IReadOnlyList<RankedCandidate> candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            Console.WriteLine($"{i + 1}. Profile ID {c.ResourceProfileId}  |  {c.Name}  |  Suggested: {c.SuggestedUtilisation}%");
            Console.WriteLine($"   Reason: {c.Reason}");
            Console.WriteLine();
        }
    }
}
