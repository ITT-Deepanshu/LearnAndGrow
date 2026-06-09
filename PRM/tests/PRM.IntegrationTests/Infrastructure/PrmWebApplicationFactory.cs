using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PRM.Application.Interfaces.Common;
using PRM.Infrastructure.Scheduler;

namespace PRM.IntegrationTests.Infrastructure;

public sealed class PrmWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=PRM_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<PrmBackgroundService>();

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock, FixedTestClock>();
        });
    }
}

internal sealed class FixedTestClock : IClock
{
    public DateTime UtcNow { get; } = new(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc);
    public DateOnly Today => new(2026, 5, 14);
}
