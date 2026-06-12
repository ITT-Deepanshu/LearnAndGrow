using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Employee;

public sealed class MyAllocationsView(AllocationsApi allocations, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("MY ALLOCATIONS");

        try
        {
            var allocationList = (await allocations.ListMyAsync(ct))
                .Where(a => a.EndedAt is null)
                .ToList();

            ui.PrintTable(
                ["Project", "%", "From", "To", "Status"],
                allocationList.Select(a => new List<string>
                {
                    a.ProjectName,
                    $"{a.UtilisationPercentage}%",
                    ui.FormatDate(a.FromDate),
                    ui.FormatDate(a.ToDate),
                    "ACTIVE"
                }));

            var total = allocationList.Sum(a => a.UtilisationPercentage);
            Console.WriteLine();
            Console.WriteLine($"Total Utilisation: {total}%");
            ui.DrawDivider();
            Console.WriteLine("[B] Back");
            ui.Prompt("Action");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }
}
