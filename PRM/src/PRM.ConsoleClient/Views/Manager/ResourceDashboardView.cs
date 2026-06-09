using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class ResourceDashboardView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();

        try
        {
            var dashboard = await api.GetResourceDashboardAsync(ct);
            ui.DrawBox($"RESOURCE DASHBOARD — {dashboard.AsOfDate:MMMM yyyy}");

            Console.WriteLine($"ON BENCH  ({dashboard.OnBench.Count} employees available)");
            ui.DrawDivider();
            ui.PrintTable(
                ["ID", "Name", "Department", "Skills"],
                dashboard.OnBench.Select(e => new List<string>
                {
                    e.Id.ToString(), e.FullName, e.Department, string.Join(", ", e.Skills)
                }));

            Console.WriteLine();
            Console.WriteLine("ACTIVE EMPLOYEES");
            ui.DrawDivider();
            var active = dashboard.PartiallyAllocated.Concat(dashboard.FullyAllocated).ToList();
            ui.PrintTable(
                ["ID", "Name", "Alloc %", "Availability"],
                active.Select(e => new List<string>
                {
                    e.Id.ToString(),
                    e.FullName,
                    $"{e.UtilisationPercentage}%",
                    e.AvailabilityPercentage <= 0 ? "FULL" : $"{e.AvailabilityPercentage}% free"
                }));

            Console.WriteLine();
            Console.WriteLine($"Bench: {dashboard.Counts.BenchCount}   |   Partial: {dashboard.Counts.PartiallyAllocatedCount}");
            ui.DrawDivider();
            Console.WriteLine("[D] Drill into employee details     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action == "D")
                await DrillDownAsync(dashboard, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task DrillDownAsync(Models.ResourceDashboard dashboard, CancellationToken ct)
    {
        var employeeId = ui.PromptLong("Enter Employee ID");
        var detail = dashboard.DrillDown.FirstOrDefault(d => d.Id == employeeId);
        if (detail is null)
        {
            try
            {
                var employee = await api.GetEmployeeAsync(employeeId, ct);
                var allocations = await api.ListAllocationsByEmployeeAsync(employeeId, ct);
                ui.ClearScreen();
                ui.DrawSection(employee.FullName);
                Console.WriteLine($"Department     : {employee.Department}");
                Console.WriteLine($"Current Status : {employee.Status}");
                Console.WriteLine($"Profile Skills : {string.Join(", ", employee.Skills.Select(s => s.Name))}");
                Console.WriteLine();
                Console.WriteLine("Active Allocations:");
                ui.PrintTable(
                    ["Project", "%", "From", "To"],
                    allocations.Where(a => a.EndedAt is null).Select(a => new List<string>
                    {
                        a.ProjectName, $"{a.UtilisationPercentage}%", ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                    }));
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); }
        }
        else
        {
            ui.ClearScreen();
            ui.DrawSection(detail.FullName);
            Console.WriteLine($"Department     : {detail.Department}");
            Console.WriteLine($"Current Status : {detail.Status} ({detail.UtilisationPercentage}%)");
            Console.WriteLine($"Profile Skills : {string.Join(", ", detail.ProfileSkills)}");
            Console.WriteLine();
            Console.WriteLine("Active Allocations:");
            ui.PrintTable(
                ["Project", "%", "From", "To"],
                detail.ActiveAllocations.Select(a => new List<string>
                {
                    a.ProjectName, $"{a.UtilisationPercentage}%", ui.FormatDate(a.FromDate), ui.FormatDate(a.ToDate)
                }));
            if (detail.RecentActivityTags.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Recent Activity Tags (last 4 weeks):");
                Console.WriteLine($"  {string.Join(", ", detail.RecentActivityTags)}");
            }
        }

        ui.Pause();
    }
}
