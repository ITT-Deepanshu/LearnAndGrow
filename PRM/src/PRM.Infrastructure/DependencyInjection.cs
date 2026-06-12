using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Scheduling;
using PRM.Application.Interfaces.Security;
using PRM.Infrastructure.Ai;
using PRM.Infrastructure.Auth;
using PRM.Infrastructure.Common;
using PRM.Infrastructure.Scheduler;
using PRM.Infrastructure.Security;

namespace PRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IApiKeyProtector, ApiKeyProtector>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<AiApiKeyResolver>();

        var gemmaTimeout = configuration.GetSection($"{AiSettings.SectionName}:Gemma").GetValue("TimeoutSeconds", 60);
        services.AddHttpClient<GemmaProvider>(client => client.Timeout = TimeSpan.FromSeconds(gemmaTimeout));
        services.AddScoped<IAiProvider, GemmaProvider>(sp => sp.GetRequiredService<GemmaProvider>());
        services.AddScoped<IAiProviderOrchestrator, AiProviderOrchestrator>();
        services.AddScoped<IPrmBackgroundJobRunner, PrmBackgroundJobRunner>();

        return services;
    }
}
