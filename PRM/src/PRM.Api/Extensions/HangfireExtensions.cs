using Hangfire;
using Hangfire.SqlServer;
using PRM.Api.Filters;
using PRM.Application.Interfaces.Scheduling;
using PRM.Infrastructure.Scheduler;

namespace PRM.Api.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddPrmHangfire(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IPrmJobScheduleManager, NoOpJobScheduleManager>();
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                SchemaName = "Hangfire",
                PrepareSchemaIfNecessary = true
            }));

        services.AddSingleton<IPrmJobScheduleManager, HangfireJobScheduleManager>();

        return services;
    }

    public static IServiceCollection AddPrmHangfireServer(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Testing"))
            services.AddHangfireServer();

        return services;
    }

    public static WebApplication UsePrmHangfireDashboard(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireAdminAuthorizationFilter()],
                DashboardTitle = "PRM Background Jobs"
            });
        }

        return app;
    }
}
