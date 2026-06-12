using PRM.ConsoleClient.Api;

using PRM.ConsoleClient.Models;

using PRM.ConsoleClient.Services;



namespace PRM.ConsoleClient.Views.Employee;



public sealed class SubmitTimesheetView(

    TimesheetsApi timesheets,

    AllocationsApi allocations,

    ConsoleUi ui,

    SessionContext session)

{

    public async Task RunAsync(CancellationToken ct = default)

    {

        ui.ClearScreen();

        ui.DrawBox("SUBMIT TIMESHEET");



        try

        {

            var context = await timesheets.GetSubmissionContextAsync(ct);

            Console.WriteLine($"Employee  : {session.FullName}");

            var weekStart = ui.PromptWeekStartOptional("Week Start: Enter date (DD-MM-YYYY) or press Enter for last Monday");



            var existing = await timesheets.GetMyForWeekAsync(weekStart, ct);

            if (existing is not null && existing.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))

            {

                ui.WriteError("A timesheet for this week has already been submitted.");

                ui.Pause();

                return;

            }



            if (existing is not null && existing.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase))

                ui.WriteWarning($"Replacing missed timesheet for week {ui.FormatDate(weekStart)}.");



            var allocationList = (await allocations.ListMyAsync(ct))

                .Where(a => a.EndedAt is null && a.FromDate <= weekStart.AddDays(6) && a.ToDate >= weekStart)

                .ToList();



            if (allocationList.Count == 0)

            {

                ui.WriteError("No active allocations found for this week.");

                ui.Pause();

                return;

            }



            Console.WriteLine();

            Console.WriteLine("Checking your active allocations for this week...");

            var entries = new List<SubmitTimesheetEntryRequest>();

            decimal totalHours = 0;



            for (var i = 0; i < allocationList.Count; i++)

            {

                var allocation = allocationList[i];

                var maxHours = allocation.UtilisationPercentage / 100m * context.MaxWeeklyHours;

                ui.DrawDivider();

                Console.WriteLine($"PROJECT {i + 1} OF {allocationList.Count} — {allocation.ProjectName}");

                Console.WriteLine($"  Allocation: {allocation.UtilisationPercentage}%   |   Expected: {maxHours:0} hrs max");

                ui.DrawDivider();



                var hours = ui.PromptDecimal("Hours worked this week");

                var tagIds = PromptActivityTags(context.ActivityTags);

                if (tagIds.Count == 0)

                {

                    ui.WriteError("At least one activity tag is required.");

                    ui.Pause();

                    return;

                }



                string? customText = null;

                if (tagIds.Any(id => context.ActivityTags.First(t => t.Id == id).IsCustom))

                    customText = ui.Prompt("Custom activity description");



                entries.Add(new SubmitTimesheetEntryRequest(allocation.ProjectId, hours, tagIds, customText));

                totalHours += hours;

            }



            ui.DrawDivider();

            Console.WriteLine("SUMMARY");

            foreach (var (entry, allocation) in entries.Zip(allocationList))

            {

                var tags = entry.TagIds.Select(id => context.ActivityTags.First(t => t.Id == id).Name);

                Console.WriteLine($"  {allocation.ProjectName,-16} {entry.Hours,4} hrs    [{string.Join(", ", tags)}]");

            }

            Console.WriteLine($"  {new string('─', 40)}");

            var status = totalHours <= context.MaxWeeklyHours ? "✓" : "⚠";

            Console.WriteLine($"  Total           {totalHours,4} hrs / {context.MaxWeeklyHours} hrs max   {status}");

            ui.DrawDivider();



            if (ui.Prompt("Action [S] Submit Timesheet  [B] Back").ToUpperInvariant() != "S") return;



            var result = await timesheets.SubmitAsync(new SubmitTimesheetRequest(weekStart, entries), ct);

            ui.WriteSuccess($"Timesheet submitted successfully. Status: {result.Status}");

        }

        catch (ApiException ex) { ui.WriteError(ex.Message); }



        ui.Pause();

    }



    private List<int> PromptActivityTags(IReadOnlyList<ActivityTagItem> tags)

    {

        Console.WriteLine("What did you work on? Select activity tags:");

        foreach (var tag in tags)

            Console.WriteLine($"  {tag.Id,2}.  {tag.Name}");



        var input = ui.Prompt("Select tags (comma-separated)");

        var validIds = tags.Select(t => t.Id).ToHashSet();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Select(s => int.TryParse(s, out var id) ? id : 0)

            .Where(validIds.Contains)

            .Distinct()

            .ToList();

    }

}


