using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views.Employee;

namespace PRM.ConsoleClient.Menus;

public sealed class EmployeeMenu(
    ConsoleUi ui,
    SessionContext session,
    SubmitTimesheetView submitTimesheet,
    MyTimesheetsView myTimesheets,
    MyAllocationsView myAllocations,
    AuthApi auth,
    TimesheetsApi timesheets)
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
                    await auth.LogoutAsync(ct);
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
            var reminder = await timesheets.GetReminderAsync(ct);
            if (reminder is null)
                return;

            ui.WriteWarning(reminder.Message);
            Console.WriteLine();
        }
        catch (ApiException)
        {
            // Reminder is best-effort; do not block the menu.
        }
    }
}
