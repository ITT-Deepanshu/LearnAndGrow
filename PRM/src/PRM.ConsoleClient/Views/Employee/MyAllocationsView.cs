using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Employee;

public sealed class MyAllocationsView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("MY ALLOCATIONS");

        try
        {
            var employeeId = await api.ResolveEmployeeIdAsync(ct);
            var allocations = (await api.ListAllocationsByEmployeeAsync(employeeId, ct))
                .Where(a => a.EndedAt is null)
                .ToList();

            ui.PrintTable(
                ["Project", "%", "From", "To", "Status"],
                allocations.Select(a => new List<string>
                {
                    a.ProjectName,
                    $"{a.UtilisationPercentage}%",
                    ui.FormatDate(a.FromDate),
                    ui.FormatDate(a.ToDate),
                    "ACTIVE"
                }));

            var total = allocations.Sum(a => a.UtilisationPercentage);
            Console.WriteLine();
            Console.WriteLine($"Total Utilisation: {total}%");
            ui.DrawDivider();
            Console.WriteLine("[B] Back");
            ui.Prompt("Action");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }
}
