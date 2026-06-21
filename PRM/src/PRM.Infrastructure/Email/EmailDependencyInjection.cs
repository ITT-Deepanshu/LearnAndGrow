using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRM.Application.Interfaces.Notifications;
using PRM.Infrastructure.Email.Providers;

namespace PRM.Infrastructure.Email;

public static class EmailDependencyInjection
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SmtpProviderOptions>(configuration.GetSection($"{EmailOptions.SectionName}:Smtp"));

        services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IEmailTemplateRenderer, HtmlEmailTemplateRenderer>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
