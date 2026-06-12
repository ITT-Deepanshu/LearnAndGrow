using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class ResourceDashboardView(
    DashboardApi dashboard,
    EmployeesApi employees,
    AllocationsApi allocations,
    ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();

        try
        {
            var data = await dashboard.GetResourcesAsync(ct);
            ui.DrawBox($"RESOURCE DASHBOARD — {data.AsOfDate:MMMM yyyy}");

            Console.WriteLine($"ON BENCH  ({data.OnBench.Count} employees available)");
            ui.DrawDivider();
            ui.PrintTable(
                ["ID", "Name", "Department", "Skills"],
                data.OnBench.Select(e => new List<string>
                {
                    e.Id.ToString(), e.FullName, e.Department, string.Join(", ", e.Skills)
                }));

            Console.WriteLine();
            Console.WriteLine("ACTIVE EMPLOYEES");
            ui.DrawDivider();
            var active = data.PartiallyAllocated.Concat(data.FullyAllocated).ToList();
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
            Console.WriteLine($"Bench: {data.Counts.BenchCount}   |   Partial: {data.Counts.PartiallyAllocatedCount}");
            ui.DrawDivider();
            Console.WriteLine("[D] Drill into resourceProfile details     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action == "D")
                await DrillDownAsync(data, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task DrillDownAsync(Models.ResourceDashboard data, CancellationToken ct)
    {
        var employeeId = ui.PromptLong("Enter resourceProfile ID");
        var detail = data.DrillDown.FirstOrDefault(d => d.Id == employeeId);
        if (detail is null)
        {
            try
            {
                var resourceProfile = await employees.GetAsync(employeeId, ct);
                var allocationList = await allocations.ListByEmployeeAsync(employeeId, ct);
                ui.ClearScreen();
                ui.DrawSection(resourceProfile.FullName);
                Console.WriteLine($"Department     : {resourceProfile.Department}");
                Console.WriteLine($"Current Status : {resourceProfile.Status}");
                Console.WriteLine($"Profile Skills : {string.Join(", ", resourceProfile.Skills.Select(s => s.Name))}");
                Console.WriteLine();
                Console.WriteLine("Active Allocations:");
                ui.PrintTable(
                    ["Project", "%", "From", "To"],
                    allocationList.Where(a => a.EndedAt is null).Select(a => new List<string>
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
