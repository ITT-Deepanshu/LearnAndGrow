using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PRM.ConsoleClient.Menus;
using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views;
using PRM.ConsoleClient.Views.Admin;
using PRM.ConsoleClient.Views.Employee;
using PRM.ConsoleClient.Views.Manager;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<SessionContext>();
        services.AddSingleton<TokenStore>();
        services.AddSingleton<ConsoleUi>();
        services.AddSingleton<ApiClient>();

        services.AddSingleton<LoginView>();
        services.AddSingleton<ChangePasswordView>();

        services.AddSingleton<ManageUsersView>();
        services.AddSingleton<ManageEmployeesView>();
        services.AddSingleton<ManageProjectsView>();
        services.AddSingleton<ViewAllocationsView>();
        services.AddSingleton<SystemConfigView>();

        services.AddSingleton<ResourceDashboardView>();
        services.AddSingleton<AllocateResourceView>();
        services.AddSingleton<MyProjectsView>();
        services.AddSingleton<TeamTimesheetsView>();
        services.AddSingleton<AiAssistantView>();

        services.AddSingleton<SubmitTimesheetView>();
        services.AddSingleton<MyTimesheetsView>();
        services.AddSingleton<MyAllocationsView>();

        services.AddSingleton<AdminMenu>();
        services.AddSingleton<ManagerMenu>();
        services.AddSingleton<EmployeeMenu>();
    })
    .Build();

var ui = host.Services.GetRequiredService<ConsoleUi>();
var loginView = host.Services.GetRequiredService<LoginView>();
var session = host.Services.GetRequiredService<SessionContext>();
var tokenStore = host.Services.GetRequiredService<TokenStore>();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    session.Clear();
    tokenStore.Clear();
    Environment.Exit(0);
};

try
{
    if (!await loginView.RunAsync())
        return;

    while (session.IsAuthenticated)
    {
        var role = session.Role?.ToUpperInvariant();
        switch (role)
        {
            case "ADMIN":
                await host.Services.GetRequiredService<AdminMenu>().RunAsync();
                break;
            case "MANAGER":
                await host.Services.GetRequiredService<ManagerMenu>().RunAsync();
                break;
            case "EMPLOYEE":
                await host.Services.GetRequiredService<EmployeeMenu>().RunAsync();
                break;
            default:
                ui.WriteError($"Unsupported role: {session.Role}");
                return;
        }

        if (!session.IsAuthenticated)
            break;

        var loggedInAgain = await loginView.RunAsync();
        if (!loggedInAgain)
            break;
    }

    ui.ClearScreen();
    Console.WriteLine("Thank you for using PRM. Goodbye!");
}
catch (Exception ex)
{
    ui.WriteError(ex.Message);
    ui.Pause();
}
