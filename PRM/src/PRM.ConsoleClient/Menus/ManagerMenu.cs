using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views.Manager;

namespace PRM.ConsoleClient.Menus;

public sealed class ManagerMenu(
    ConsoleUi ui,
    SessionContext session,
    ResourceDashboardView resourceDashboard,
    AllocateResourceView allocateResource,
    MyProjectsView myProjects,
    TeamTimesheetsView teamTimesheets,
    TeamSkillMatchView teamBuilder,
    AiAssistantView aiAssistant,
    AuthApi auth)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox($"Welcome, {session.FullName}!  |  {ui.FormatNow()}");

            Console.WriteLine("1. Resource Dashboard");
            Console.WriteLine("2. Allocate Resource");
            Console.WriteLine("3. My Projects");
            Console.WriteLine("4. Timesheets");
            Console.WriteLine("5. Team Builder    — Create a whole team from one request");
            Console.WriteLine("6. AI Assistant");
            Console.WriteLine("7. Logout");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await resourceDashboard.RunAsync(ct); break;
                case "2": await allocateResource.RunAsync(ct); break;
                case "3": await myProjects.RunAsync(ct); break;
                case "4": await teamTimesheets.RunAsync(ct); break;
                case "5": await teamBuilder.RunAsync(ct); break;
                case "6": await aiAssistant.RunAsync(ct); break;
                case "7":
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
