using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views.Admin;

namespace PRM.ConsoleClient.Menus;

public sealed class AdminMenu(
    ConsoleUi ui,
    SessionContext session,
    ManageUsersView manageUsers,
    ManageEmployeesView manageEmployees,
    ManageProjectsView manageProjects,
    ViewAllocationsView viewAllocations,
    SystemConfigView systemConfig,
    AuthApi auth)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox(
                "ADMIN PANEL",
                $"Welcome, {session.FullName}  |  {ui.FormatNow()}");

            Console.WriteLine("1. Manage Resources");
            Console.WriteLine("2. Manage Projects");
            Console.WriteLine("3. View All Allocations");
            Console.WriteLine("4. Manage Users");
            Console.WriteLine("5. System Configuration");
            Console.WriteLine("6. Logout");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await manageEmployees.RunAsync(ct); break;
                case "2": await manageProjects.RunAsync(ct); break;
                case "3": await viewAllocations.RunAsync(ct); break;
                case "4": await manageUsers.RunAsync(ct); break;
                case "5": await systemConfig.RunAsync(ct); break;
                case "6":
                    await auth.LogoutAsync(ct);
                    return;
                default:
                    ui.WriteError("Invalid option.");
                    ui.Pause();
                    break;
            }
        }
    }
}
