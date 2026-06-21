using Microsoft.Extensions.DependencyInjection;
using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Menus;
using PRM.ConsoleClient.Services;
using PRM.ConsoleClient.Views;
using PRM.ConsoleClient.Views.Admin;
using PRM.ConsoleClient.Views.Employee;
using PRM.ConsoleClient.Views.Manager;

namespace PRM.ConsoleClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleClient(this IServiceCollection services)
    {
        // Core
        services.AddSingleton<SessionContext>();
        services.AddSingleton<TokenStore>();
        services.AddSingleton<ConsoleUi>();
        services.AddSingleton<PrmHttpClient>();

        // Feature APIs (one per API area — mirrors PRM.Api controllers)
        services.AddSingleton<AuthApi>();
        services.AddSingleton<UsersApi>();
        services.AddSingleton<EmployeesApi>();
        services.AddSingleton<ProjectsApi>();
        services.AddSingleton<AllocationsApi>();
        services.AddSingleton<TimesheetsApi>();
        services.AddSingleton<DashboardApi>();
        services.AddSingleton<AiApi>();
        services.AddSingleton<SystemConfigApi>();

        // Views
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
        services.AddSingleton<TeamSkillMatchView>();
        services.AddSingleton<SubmitTimesheetView>();
        services.AddSingleton<MyTimesheetsView>();
        services.AddSingleton<MyAllocationsView>();

        // Menus
        services.AddSingleton<AdminMenu>();
        services.AddSingleton<ManagerMenu>();
        services.AddSingleton<EmployeeMenu>();

        return services;
    }
}
