using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views.Employee;

namespace PRM.ConsoleClient.Menus;

public sealed class EmployeeMenu(
    ConsoleUi ui,
    SessionContext session,
    SubmitTimesheetView submitTimesheet,
    MyTimesheetsView myTimesheets,
    MyAllocationsView myAllocations,
    ApiClient api)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox($"Welcome, {session.FullName}!  |  {ui.FormatNow()}");

            await ShowMissedReminderAsync(ct);

            Console.WriteLine("1. Submit Timesheet");
            Console.WriteLine("2. View My Timesheets");
            Console.WriteLine("3. View My Allocations");
            Console.WriteLine("4. Logout");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await submitTimesheet.RunAsync(ct); break;
                case "2": await myTimesheets.RunAsync(ct); break;
                case "3": await myAllocations.RunAsync(ct); break;
                case "4":
                    await api.LogoutAsync(ct);
                    return;
                default:
                    ui.WriteError("Invalid option.");
                    ui.Pause();
                    break;
            }
        }
    }

    private async Task ShowMissedReminderAsync(CancellationToken ct)
    {
        try
        {
            var timesheets = await api.ListMyTimesheetsAsync(ct);
            var previousWeek = ui.GetPreviousCompletedWeekMonday();
            var missed = timesheets.FirstOrDefault(t =>
                t.WeekStart == previousWeek &&
                t.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase));

            if (missed is not null)
            {
                ui.WriteWarning($"Reminder: Timesheet for week {ui.FormatDate(previousWeek)} has not been submitted.");
                Console.WriteLine();
            }
        }
        catch
        {
            // Reminder is best-effort; do not block the menu.
        }
    }
}
