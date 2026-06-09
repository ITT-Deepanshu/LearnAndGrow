using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class ViewAllocationsView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        long? employeeFilter = null;
        long? projectFilter = null;

        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("ALL ALLOCATIONS");

            try
            {
                var allocations = await api.ListAllocationsAsync(employeeFilter, projectFilter, ct);
                ui.PrintTable(
                    ["Employee", "Project", "%", "From", "To"],
                    allocations.Select(a => new List<string>
                    {
                        a.EmployeeName,
                        a.ProjectName,
                        $"{a.UtilisationPercentage}%",
                        ui.FormatDate(a.FromDate),
                        ui.FormatDate(a.ToDate)
                    }));

                Console.WriteLine();
                Console.WriteLine($"Total Active Allocations: {allocations.Count}");
                ui.DrawDivider();
                Console.WriteLine("[F] Filter by Employee / Project     [B] Back");
                var action = ui.Prompt("Action").ToUpperInvariant();
                if (action == "B") return;
                if (action == "F")
                {
                    var empInput = ui.PromptOptional("Employee ID filter (blank to clear)");
                    employeeFilter = long.TryParse(empInput, out var eid) ? eid : null;
                    var projInput = ui.PromptOptional("Project ID filter (blank to clear)");
                    projectFilter = long.TryParse(projInput, out var pid) ? pid : null;
                }
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); return; }
        }
    }
}
