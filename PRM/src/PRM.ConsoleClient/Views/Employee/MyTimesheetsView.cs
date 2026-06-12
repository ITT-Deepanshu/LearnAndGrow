using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Employee;

public sealed class MyTimesheetsView(TimesheetsApi timesheets, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("MY TIMESHEETS");

        try
        {
            var timesheetList = await timesheets.ListMyAsync(ct);
            ui.PrintTable(
                ["Week Start", "Total Hrs", "Status"],
                timesheetList.Select(t => new List<string>
                {
                    ui.FormatDate(t.WeekStart),
                    $"{t.TotalHours:0} hrs",
                    t.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase) ? $"{t.Status}    ⚠" : t.Status
                }));

            ui.DrawDivider();
            Console.WriteLine("[V] View week details     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();
            if (action == "V")
                await ViewWeekDetailAsync(timesheetList, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ViewWeekDetailAsync(IReadOnlyList<Models.TimesheetListItem> timesheetList, CancellationToken ct)
    {
        if (timesheetList.Count == 0) return;

        for (var i = 0; i < timesheetList.Count; i++)
            Console.WriteLine($"{i + 1}. {ui.FormatDate(timesheetList[i].WeekStart)} — {timesheetList[i].Status}");

        var choice = ui.PromptInt("Select week #", 1, timesheetList.Count) - 1;
        var week = timesheetList[choice].WeekStart;

        try
        {
            var detail = await timesheets.GetMyForWeekAsync(week, ct);
            ui.ClearScreen();
            ui.DrawSection($"Week: {ui.FormatDate(week)} — Status: {timesheetList[choice].Status}");

            if (detail is null || detail.Entries.Count == 0)
            {
                Console.WriteLine("No entries recorded for this week.");
            }
            else
            {
                ui.PrintTable(
                    ["Project", "Hrs", "Activity Tags"],
                    detail.Entries.Select(e => new List<string>
                    {
                        e.ProjectName, e.Hours.ToString("0.##"), string.Join(", ", e.ActivityTags)
                    }));
                Console.WriteLine();
                Console.WriteLine($"Total: {detail.TotalHours:0} hrs");
            }
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }
}
