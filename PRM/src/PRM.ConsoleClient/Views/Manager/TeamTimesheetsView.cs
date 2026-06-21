using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Manager;

public sealed class TeamTimesheetsView(TimesheetsApi timesheets, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("TIMESHEETS — MY TEAM");

        var weekStart = ui.PromptWeekStartOptional("Filter by week (DD-MM-YYYY) or press Enter for current week");

        try
        {
            var rows = await timesheets.ListTeamAsync(weekStart, ct);
            Console.WriteLine($"Week: {ui.FormatDate(weekStart)}");
            ui.DrawDivider();
            ui.PrintTable(
                ["Employee", "Project", "Hrs", "Status", "Frozen"],
                rows.Select(r => new List<string>
                {
                    r.EmployeeName,
                    r.ProjectName,
                    r.Hours.ToString("0.##"),
                    FormatStatus(r.Status),
                    r.SubmissionFrozen ? "Yes" : "No"
                }));

            ui.DrawDivider();
            Console.WriteLine("[V] View employee detail     [R] Restore submission     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action == "V")
                ShowEmployeeDetail(rows);
            else if (action == "R")
                await RestoreSubmissionAsync(rows, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }

    private static string FormatStatus(string status) =>
        status.Equals("Missed", StringComparison.OrdinalIgnoreCase) ? $"{status} ⚠" : status;

    private void ShowEmployeeDetail(IReadOnlyList<Models.TeamTimesheetRow> rows)
    {
        if (rows.Count == 0)
        {
            ui.WriteError("No timesheet rows for this week.");
            return;
        }

        var employees = rows.Select(r => r.EmployeeName).Distinct().ToList();
        foreach (var employee in employees)
            Console.WriteLine($"  {employee}");

        var name = ui.Prompt("Enter employee name");
        var employeeRows = rows
            .Where(r => r.EmployeeName.Equals(name, StringComparison.OrdinalIgnoreCase)
                || r.EmployeeName.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (employeeRows.Count == 0)
        {
            ui.WriteError("Employee not found.");
            return;
        }

        ui.ClearScreen();
        ui.DrawSection($"Timesheet — {employeeRows[0].EmployeeName}");
        ui.PrintTable(
            ["Project", "Hrs", "Status", "Frozen"],
            employeeRows.Select(r => new List<string>
            {
                r.ProjectName,
                r.Hours.ToString("0.##"),
                FormatStatus(r.Status),
                r.SubmissionFrozen ? "Yes" : "No"
            }));
    }

    private async Task RestoreSubmissionAsync(IReadOnlyList<Models.TeamTimesheetRow> rows, CancellationToken ct)
    {
        var frozen = rows
            .Where(r => r.SubmissionFrozen)
            .GroupBy(r => r.ResourceProfileId)
            .Select(g => g.First())
            .ToList();

        if (frozen.Count == 0)
        {
            ui.WriteError("No employees with restricted timesheet submission.");
            return;
        }

        foreach (var row in frozen)
            Console.WriteLine($"  {row.EmployeeName} (id {row.ResourceProfileId})");

        var name = ui.Prompt("Enter employee name to restore");
        var match = frozen.FirstOrDefault(r =>
            r.EmployeeName.Equals(name, StringComparison.OrdinalIgnoreCase)
            || r.EmployeeName.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            ui.WriteError("Frozen employee not found.");
            return;
        }

        await timesheets.RestoreSubmissionAsync(match.ResourceProfileId, ct);
        ui.WriteSuccess($"Restored timesheet submission for {match.EmployeeName}.");
    }
}
