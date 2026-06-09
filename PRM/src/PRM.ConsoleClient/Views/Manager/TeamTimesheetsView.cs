using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class TeamTimesheetsView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("TIMESHEETS — MY TEAM");

        var weekInput = ui.PromptOptional("Filter by week (DD-MM-YYYY) or press Enter for current week");
        var weekStart = string.IsNullOrWhiteSpace(weekInput)
            ? ui.GetLastMonday()
            : ui.TryParseDateInput(weekInput, out var parsed) ? parsed : ui.PromptDate("Week (DD-MM-YYYY)");

        try
        {
            var rows = await api.ListTeamTimesheetsAsync(weekStart, ct);
            Console.WriteLine($"Week: {ui.FormatDate(weekStart)}");
            ui.DrawDivider();
            ui.PrintTable(
                ["Employee", "Project", "Hrs", "Status"],
                rows.Select(r => new List<string>
                {
                    r.EmployeeName, r.ProjectName, r.Hours.ToString("0.##"),
                    r.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase) ? $"{r.Status} ⚠" : r.Status
                }));

            ui.DrawDivider();
            Console.WriteLine("[V] View employee timesheet detail     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();
            if (action == "V")
                ShowEmployeeDetail(rows);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }

    private void ShowEmployeeDetail(IReadOnlyList<Models.TeamTimesheetRow> rows)
    {
        var employees = rows.Select(r => r.EmployeeName).Distinct().ToList();
        for (var i = 0; i < employees.Count; i++)
            Console.WriteLine($"{i + 1}. {employees[i]}");

        var choice = ui.PromptInt("Select employee #", 1, employees.Count) - 1;
        var name = employees[choice];
        var employeeRows = rows.Where(r => r.EmployeeName == name).ToList();

        ui.ClearScreen();
        ui.DrawSection($"Timesheet — {name}");
        ui.PrintTable(
            ["Project", "Hrs", "Status"],
            employeeRows.Select(r => new List<string>
            {
                r.ProjectName, r.Hours.ToString("0.##"), r.Status
            }));
    }
}
