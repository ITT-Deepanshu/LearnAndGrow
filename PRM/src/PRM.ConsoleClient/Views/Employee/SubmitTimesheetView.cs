using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Employee;

public sealed class SubmitTimesheetView(ApiClient api, ConsoleUi ui, SessionContext session)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ui.ClearScreen();
        ui.DrawBox("SUBMIT TIMESHEET");

        try
        {
            Console.WriteLine($"Employee  : {session.FullName}");
            var weekInput = ui.PromptOptional("Week Start: Enter date (DD-MM-YYYY) or press Enter for last Monday");
            var weekStart = string.IsNullOrWhiteSpace(weekInput)
                ? ui.GetLastMonday()
                : ui.TryParseDateInput(weekInput, out var parsed) ? parsed : ui.PromptDate("Week Start (DD-MM-YYYY)");

            var existing = await api.GetMyTimesheetForWeekAsync(weekStart, ct);
            if (existing is not null)
            {
                ui.WriteError("A timesheet for this week has already been submitted.");
                ui.Pause();
                return;
            }

            var employeeId = await api.ResolveEmployeeIdAsync(ct);
            var allocations = (await api.ListAllocationsByEmployeeAsync(employeeId, ct))
                .Where(a => a.EndedAt is null && a.FromDate <= weekStart.AddDays(6) && a.ToDate >= weekStart)
                .ToList();

            if (allocations.Count == 0)
            {
                ui.WriteError("No active allocations found for this week.");
                ui.Pause();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Checking your active allocations for this week...");
            var entries = new List<SubmitTimesheetEntryRequest>();
            decimal totalHours = 0;

            for (var i = 0; i < allocations.Count; i++)
            {
                var allocation = allocations[i];
                var maxHours = allocation.UtilisationPercentage / 100m * Constants.DefaultMaxWeeklyHours;
                ui.DrawDivider();
                Console.WriteLine($"PROJECT {i + 1} OF {allocations.Count} — {allocation.ProjectName}");
                Console.WriteLine($"  Allocation: {allocation.UtilisationPercentage}%   |   Expected: {maxHours:0} hrs max");
                ui.DrawDivider();

                var hours = ui.PromptDecimal("Hours worked this week");
                var tagIds = PromptActivityTags();
                string? customText = null;
                if (tagIds.Contains(11))
                    customText = ui.Prompt("Custom activity description");

                entries.Add(new SubmitTimesheetEntryRequest(allocation.ProjectId, hours, tagIds, customText));
                totalHours += hours;
            }

            ui.DrawDivider();
            Console.WriteLine("SUMMARY");
            foreach (var (entry, allocation) in entries.Zip(allocations))
            {
                var tags = entry.TagIds.Select(id => Constants.ActivityTags.First(t => t.Id == id).Name);
                Console.WriteLine($"  {allocation.ProjectName,-16} {entry.Hours,4} hrs    [{string.Join(", ", tags)}]");
            }
            Console.WriteLine($"  {new string('─', 40)}");
            var status = totalHours <= Constants.DefaultMaxWeeklyHours ? "✓" : "⚠";
            Console.WriteLine($"  Total           {totalHours,4} hrs / {Constants.DefaultMaxWeeklyHours} hrs max   {status}");
            ui.DrawDivider();

            if (ui.Prompt("Action [S] Submit Timesheet  [B] Back").ToUpperInvariant() != "S") return;

            var result = await api.SubmitTimesheetAsync(new SubmitTimesheetRequest(weekStart, entries), ct);
            ui.WriteSuccess($"Timesheet submitted successfully. Status: {result.Status}");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }

        ui.Pause();
    }

    private List<int> PromptActivityTags()
    {
        Console.WriteLine("What did you work on? Select activity tags:");
        for (var i = 0; i < Constants.ActivityTags.Count; i++)
            Console.WriteLine($"  {Constants.ActivityTags[i].Id,2}.  {Constants.ActivityTags[i].Name}");

        var input = ui.Prompt("Select tags (comma-separated)");
        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id is >= 1 and <= 11)
            .Distinct()
            .ToList();
    }
}
