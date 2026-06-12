using System.Reflection;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using PRM.Application.Ai;

using PRM.Application.Allocations;

using PRM.Application.Auth;

using PRM.Application.Common;

using PRM.Application.Dashboard;

using PRM.Application.Employees;

using PRM.Application.Projects;

using PRM.Application.SystemConfig;

using PRM.Application.Timesheets;

using PRM.Application.Users;

using PRM.Domain.Services;



namespace PRM.Application;



public static class DependencyInjection

{

    public static IServiceCollection AddApplication(this IServiceCollection services)

    {

        services.AddValidatorsFromAssembly(
            Assembly.GetExecutingAssembly(),
            filter: type => type.ValidatorType != typeof(CreateUserPasswordValidator));



        services.AddScoped<ProjectHealthDomainService>();
        services.AddScoped<ProjectEffortBuilder>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IEmployeeService, EmployeeService>();

        services.AddScoped<IProjectService, ProjectService>();

        services.AddScoped<IAllocationService, AllocationService>();

        services.AddScoped<ITimesheetService, TimesheetService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IAiService, AiService>();

        services.AddScoped<ISystemConfigService, SystemConfigService>();



        return services;

    }

}


